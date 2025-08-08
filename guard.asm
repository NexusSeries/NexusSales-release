; guard.asm - Secure string decryption for NexusSales.UI.exe
; Exports: DecryptString (for P/Invoke)
; Implements: Key generation, Base64 decode, XOR decryption, UTF-8 to UTF-16

includelib kernel32.lib
includelib ole32.lib
includelib advapi32.lib

EXTERN GetVersionExW:PROC
EXTERN GetComputerNameExW:PROC
EXTERN GetUserNameW:PROC
EXTERN MultiByteToWideChar:PROC
EXTERN WideCharToMultiByte:PROC
EXTERN CoTaskMemAlloc:PROC

.data
COMP_NAME_BUFFER_SIZE EQU 260*2
USER_NAME_BUFFER_SIZE EQU 260*2
KEY_MAX_LEN EQU 128
B64_MAX_BYTES EQU 1024
UTF16_MAX_CHARS EQU 1024

compNameBuffer      db COMP_NAME_BUFFER_SIZE dup(0)
userNameBuffer      db USER_NAME_BUFFER_SIZE dup(0)
osMajorBuffer       db 8 dup(0)
rawKeyBuffer        db 64 dup(0)
shiftedKeyBuffer    db KEY_MAX_LEN dup(0)
keyUtf8Buffer       db KEY_MAX_LEN dup(0)
base64DecodeBuffer  db B64_MAX_BYTES dup(0)
decryptedBuffer     db B64_MAX_BYTES dup(0)
utf16Buffer         dw UTF16_MAX_CHARS dup(0)
Base64DecodeMap     db 256 dup(0)

.code

PUBLIC DllMain
DllMain PROC
    mov     eax, 1
    ret
DllMain ENDP

; Safe string copy: rdi=dest, rsi=src, rcx=max bytes
SafeStringCopy PROC
    test rcx, rcx
    jz SStrDone
SStrLoop:
    mov al, [rsi]
    mov [rdi], al
    inc rsi
    inc rdi
    dec rcx
    test al, al
    jz SStrDone
    jnz SStrLoop
SStrDone:
    ret
SafeStringCopy ENDP

PUBLIC DecryptString
DecryptString PROC
    test rcx, rcx
    jz ReturnNullPtr
    sub rsp, 40
    call ObfuscatedGuardDllDecryptFunction
    add rsp, 40
    ret
ReturnNullPtr:
    xor rax, rax
    ret
DecryptString ENDP

PUBLIC ObfuscatedGuardDllDecryptFunction
ObfuscatedGuardDllDecryptFunction PROC
    ; rcx = pointer to UTF-16 input string (from C#)
    mov rdx, offset base64DecodeBuffer
    mov r8d, B64_MAX_BYTES
    call SafeBase64Decode
    test rax, rax
    jz ReturnNullPtr
    cmp rax, B64_MAX_BYTES
    ja ReturnNullPtr
    mov r8, rax

    mov rcx, offset base64DecodeBuffer
    mov rdx, offset shiftedKeyBuffer
    mov r8d, KEY_MAX_LEN
    call GenerateKey
    mov r9, rax

    mov rcx, offset base64DecodeBuffer
    mov rdx, offset shiftedKeyBuffer
    mov r8, rax
    mov r9, rax
    call PerformDecryption

    mov rcx, offset decryptedBuffer
    mov rdx, r8
    call ConvertBytesToUtf8String
    ret
ReturnNullPtr:
    xor rax, rax
    ret
ObfuscatedGuardDllDecryptFunction ENDP

; rcx=input UTF-16, rdx=output buffer, r8=max output bytes
SafeBase64Decode PROC
    mov rsi, rcx
    mov rdi, rdx
    mov rbx, r8
    xor r8, r8
    xor r9, r9
    xor r10, r10
    xor r11, r11
    lea r12, Base64DecodeMap
B64Loop:
    cmp r8, rbx
    jae B64Done
    mov ax, word ptr [rsi+r11*2]
    test ax, ax
    jz B64Done
    cmp al, '='
    je B64Pad
    mov bl, [r12+rax]
    shl r9, 6
    or r9, rbx
    add r10, 6
    inc r11
    cmp r10, 24
    jl B64Loop
    mov al, byte ptr [r9+2]
    mov [rdi+r8], al
    inc r8
    mov al, byte ptr [r9+1]
    mov [rdi+r8], al
    inc r8
    mov al, byte ptr [r9]
    mov [rdi+r8], al
    inc r8
    xor r9, r9
    xor r10, r10
    jmp B64Loop
B64Pad:
    inc r11
    cmp r10, 12
    je B64One
    cmp r10, 18
    je B64Two
    jmp B64Done
B64One:
    cmp r8, rbx
    jae B64Done
    mov al, byte ptr [r9+2]
    mov [rdi+r8], al
    inc r8
    jmp B64Done
B64Two:
    cmp r8, rbx
    jae B64Done
    mov al, byte ptr [r9+2]
    mov [rdi+r8], al
    inc r8
    mov al, byte ptr [r9+1]
    mov [rdi+r8], al
    inc r8
    jmp B64Done
B64Done:
    mov rax, r8
    ret
SafeBase64Decode ENDP

; rcx=input buffer, rdx=output buffer, r8=max bytes
PUBLIC GenerateKey
GenerateKey PROC
    mov rsi, rcx
    mov rdi, rdx
    mov rcx, r8
    call SafeStringCopy
    mov rax, r8
    ret
GenerateKey ENDP

; rcx=encrypted, rdx=key, r8=encLen, r9=keyLen
PUBLIC PerformDecryption
PerformDecryption PROC
    mov rsi, rcx
    mov rdi, offset decryptedBuffer
    mov rcx, r8
    test rcx, rcx
    jz PDone
PDLoop:
    mov al, [rsi]
    mov bl, [rdx]
    xor al, bl
    mov [rdi], al
    inc rsi
    inc rdi
    inc rdx
    dec rcx
    jnz PDLoop
PDone:
    mov rax, offset decryptedBuffer
    ret
PerformDecryption ENDP

; rcx=decryptedBuffer, rdx=len
PUBLIC ConvertBytesToUtf8String
ConvertBytesToUtf8String PROC
    ; rcx = decryptedBuffer, rdx = len (number of bytes, UTF-8)
    ; Convert UTF-8 to UTF-16 using MultiByteToWideChar, then allocate with CoTaskMemAlloc

    ; Step 1: Get required UTF-16 size (number of WCHARs)
    mov     r8d, edx            ; cbMultiByte = (int)len
    mov     r9, 0               ; lpWideCharStr = NULL (query size)
    mov     eax, 65001          ; CodePage = CP_UTF8
    mov     ecx, eax            ; CodePage in ecx
    mov     r10, rcx            ; lpMultiByteStr = decryptedBuffer
    sub     rsp, 40             ; shadow space for Win64
    mov     rcx, ecx            ; CodePage
    mov     rdx, r10            ; lpMultiByteStr = decryptedBuffer
    mov     r8d, edx            ; cbMultiByte = (int)len
    mov     r9, 0               ; lpWideCharStr = NULL
    mov     dword ptr [rsp+32], 0 ; cchWideChar = 0
    call    MultiByteToWideChar
    mov     esi, eax            ; esi = required WCHAR count
    test    esi, esi
    jz      FailCBTUS

    ; Step 2: Allocate memory for UTF-16 string (WCHARs + null terminator)
    lea     ecx, [esi+1]
    shl     ecx, 1              ; bytes = (WCHAR count + 1) * 2
    mov     edx, ecx
    mov     rcx, rdx
    call    CoTaskMemAlloc
    test    rax, rax
    jz      FailCBTUS
    mov     rdi, rax            ; rdi = allocated memory

    ; Step 3: Convert UTF-8 to UTF-16
    mov     ecx, 65001          ; CodePage = CP_UTF8
    mov     rdx, r10            ; lpMultiByteStr = decryptedBuffer
    mov     r8d, edx            ; cbMultiByte = (int)len
    mov     r9, rdi             ; lpWideCharStr = allocated memory
    mov     dword ptr [rsp+32], esi ; cchWideChar = required WCHAR count
    call    MultiByteToWideChar

    ; Step 4: Null-terminate
    mov     eax, eax            ; ensure eax is 32-bit (number of WCHARs written)
    movzx   rdx, eax            ; zero-extend eax to rdx (64-bit)
    mov     word ptr [rdi + rdx*2], 0

    add     rsp, 40
    mov     rax, rdi
    ret

FailCBTUS:
    add     rsp, 40
    xor     rax, rax
    ret
ConvertBytesToUtf8String ENDP

END