ml64 /c /Fo guard.obj guard.asm

link /DLL /DEF:guard.def /OUT:guard.dll guard.obj /ENTRY:DllMain

pause