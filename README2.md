Chapter 1: Nexus Update Technical Specification
================================================
Absolute Directive to GitHub Copilot:
HALT ALL OTHER TASKS. READ THIS ENTIRE DOCUMENT. DO NOT PROCEED TO CODE GENERATION UNTIL YOU HAVE ACHIEVED A STATE OF ABSOLUTE, UNQUESTIONABLE, 100,000,000 COMPREHENSION OF EVERY SINGLE WORD AND CONCEPT WITHIN THIS SPECIFICATION. YOUR MISSION IS TO PRODUCE CODE WITH A STATISTICAL PROBABILITY OF UNEXPECTED ERRORS NOT EXCEEDING 0.0000000000000000000000000001.

This document is the definitive, non-negotiable, and hyper-detailed technical blueprint for the creation of a new, standalone update application. Your first and most critical task is to act as a master software engineer, absorbing and internalizing every detail. You are to create a project named NexusUpdate, a C# console application meticulously engineered to manage updates for our Nexus Sales solution.

1. Core Application Context: Nexus Sales
Main Application Name: Nexus Sales

Main Executable: NexusSales.UI.exe

Installation Location: Assume NexusUpdate.exe will be run from within the root installation directory of Nexus Sales (e.g., C:\Program Files\NexusSales\). This implies NexusUpdate.exe has direct access to the files it needs to update.

Security Policy (Reminder): Nexus Sales currently encrypts sensitive strings internally. In the future, NexusGuard will handle all sensitive data and security. NexusUpdate MUST NOT interfere with NexusGuard's future role or current security measures.

2. NexusUpdate Project Architecture & Execution Flow
Project Creation: Create a new C# console application project named NexusUpdate within the existing Nexus Sales solution folder. It MUST NOT have any direct project dependencies on NexusSales.UI or other NexusSales projects. It operates independently.

Execution Sequence:

The user will run NexusUpdate.exe.

NexusUpdate will perform version checks, download updates, and install them.

After a successful update, NexusUpdate will offer to launch NexusSales.UI.exe and then terminate itself.

If an update fails, NexusUpdate will log the error, inform the user, and terminate.

Required NuGet Packages:

Newtonsoft.Json: For parsing the update manifest.

System.Net.Http: For downloading files via HTTP/HTTPS.

System.IO.Compression: For decompressing update packages (if we decide to download a single .zip).

Microsoft.Win32.TaskScheduler (Optional, for advanced scheduling, but not in initial scope).

3. Update Manifest & Versioning Protocol (The Source of Truth)
All update information will be managed via a update_manifest.json file hosted on a GitHub repository.

Online Manifest Location: Assume update_manifest.json will be hosted at a publicly accessible URL on GitHub (e.g., https://raw.githubusercontent.com/YourOrg/YourRepo/main/releases/update_manifest.json).

update_manifest.json Structure: This file will contain the latest application version and a detailed list of all files in that version.

{
  "LatestVersion": "1.0.1",
  "ReleaseNotesUrl": "https://github.com/YourOrg/YourRepo/releases/tag/v1.0.1",
  "Files": [
    {
      "Path": "NexusSales.UI.exe",
      "Hash": "a1b2c3d4e5f6...", // SHA256 hash of the file
      "Size": 1234567,            // Size in bytes
      "DownloadUrl": "https://raw.githubusercontent.com/YourOrg/YourRepo/main/releases/1.0.1/NexusSales.UI.exe",
      "Description": "Main application executable."
    },
    {
      "Path": "NexusSales.Core.dll",
      "Hash": "f6e5d4c3b2a1...",
      "Size": 7654321,
      "DownloadUrl": "https://raw.githubusercontent.com/YourOrg/YourRepo/main/releases/1.0.1/NexusSales.Core.dll",
      "Description": "Core logic library. Located in the root application directory."
    },
    {
      "Path": "Assets/logo.png", // Example of a file in a subfolder
      "Hash": "1a2b3c4d5e6f...",
      "Size": 98765,
      "DownloadUrl": "https://raw.githubusercontent.com/YourOrg/YourRepo/main/releases/1.0.1/Assets/logo.png",
      "Description": "Application logo image. Must be placed in the 'Assets' subfolder."
    }
    // ... more files
  ]
}

Version Comparison Logic:

Local Version: The updater will need to determine the currently installed version of Nexus Sales. This can be read from the Assembly.GetExecutingAssembly().GetName().Version of NexusSales.UI.exe (assuming NexusUpdate.exe is launched from the same directory).

Online Version: Download update_manifest.json from GitHub and parse LatestVersion.

Comparison: If OnlineVersion > LocalVersion, an update is available. Use System.Version for robust version comparison.

4. File Download & Integrity Verification Protocol
This module ensures that downloaded files are authentic and untampered.

Download Strategy:

For each file listed in the update_manifest.json that needs to be updated (either its hash/size differs, or it's a new file not present locally), download it directly from its DownloadUrl.

Use HttpClient for efficient, asynchronous downloads.

SHA256 Verification (CRITICAL):

After each file is downloaded, compute its SHA256 hash.

Compare this computed hash with the Hash value specified in the update_manifest.json.

Violation Protocol: If the hashes do not match, ABORT THE ENTIRE UPDATE PROCESS IMMEDIATELY. Log a critical error (tampering detected) and inform the user. Do not proceed with installation. This is a non-negotiable security check.

Temporary Storage: Download files to a secure, temporary directory (e.g., Path.GetTempPath()) before moving them to the final installation location.

5. Installation Logic & File Management
This module handles the precise placement and replacement of files.

Application Shutdown: Before replacing any files, NexusUpdate must attempt to gracefully shut down NexusSales.UI.exe if it's running.

Use Process.GetProcessesByName("NexusSales.UI") to find instances.

Attempt process.CloseMainWindow() first for a graceful exit.

If the process doesn't exit within a timeout (e.g., 5 seconds), then use process.Kill() as a forceful termination.

File Operations:

Iterate Manifest: Loop through each file entry in the downloaded update_manifest.json.

Target Path Determination: For each file, construct its full target path within the Nexus Sales installation directory, respecting the Path field (e.g., [InstallDir]/NexusSales.UI.exe, [InstallDir]/Assets/logo.png).

Directory Creation: If the target subdirectory (e.g., Assets/) does not exist, create it.

File Replacement/Addition:

If the file already exists locally and its hash/size differs from the manifest, overwrite it.

If the file does not exist locally, copy it from the temporary download location.

File Deletion (Optional but Recommended): If a file exists locally but is not present in the new update_manifest.json, it should be deleted. This handles cases where files are removed in newer versions.

Permissions Handling: Be prepared to handle UnauthorizedAccessException if NexusUpdate does not have sufficient permissions to write to the Program Files directory. The updater should prompt for elevated privileges (UAC) if necessary.

6. Mandatory Error Handling, Logging, and Debugging Protocol (Hyper-Detailed)
This phase is paramount for ensuring NexusUpdate's reliability and maintainability. NexusUpdate must be built with a "fail-early, debug-rich" philosophy.

Extensive Exception Handling: Every single method, every critical block of code, and ideally every line of code that performs an operation that could fail (file I/O, network requests, JSON parsing, hash computation, process manipulation, registry access) MUST be wrapped in a try...catch block.

Structured Error Logging (CRITICAL): When an exception is caught, do not simply print a generic message. The logging mechanism must capture and output to a dedicated log file (e.g., NexusUpdate.log in the application's temporary directory) and to the console:

DateTime: Exact timestamp (e.g., yyyy-MM-dd HH:mm:ss.fff).

LogLevel: INFO, WARNING, ERROR, FATAL.

ModuleName: The specific module/class (e.g., FileIntegrity, NetworkDownloader).

MethodName: The precise method where the error originated.

ErrorMessage: The full Exception.Message.

StackTrace: The complete Exception.StackTrace.

ErrorCode: A unique, descriptive error code or identifier (e.g., UPD-FILE-001 for file not found during hash check, UPD-NET-002 for download failure).

ContextualData: Any relevant variables or state that led to the error (e.g., filePath, downloadUrl, expectedHash, actualSize).

Debug-Level Logging: Implement a robust logging system that can be toggled (e.g., via a command-line argument or simple config file) between a silent "production" mode and a verbose "debug" mode. In debug mode, log every significant step, every successful operation, and the values of key variables. This is crucial for tracing execution flow and identifying subtle bugs.

Example: "INFO: Starting update check...", "DEBUG: Downloading file: NexusSales.UI.exe...", "SUCCESS: File NexusSales.UI.exe downloaded and hash verified."

Post-Error State: After logging a critical error, the application must immediately:

Display a user-friendly error message box with the error code and a brief explanation.

Terminate itself. There is no recovery from a failed update. The only acceptable response is a total shutdown to prevent a corrupted installation.

Code Example (Illustrative of the required detail):

// [EXPLANATION: This method attempts to download a file from a given URL.]
// [PURPOSE: To retrieve application update components from the remote repository.]
// [USAGE: Called by the main update orchestration logic for each file in the manifest.]
// [CALLERS: UpdateOrchestrator.DownloadFiles()]
private async Task<byte[]> DownloadFileAsync(string url, string filePath)
{
    // [EXPLANATION: Declare HttpClient for making HTTP requests.]
    // [PURPOSE: To facilitate network communication for file downloads.]
    // [USAGE: Used for GET requests to the GitHub raw content URL.]
    // [CALLERS: Internal to DownloadFileAsync.]
    using (HttpClient client = new HttpClient())
    {
        try
        {
            // [EXPLANATION: Send an asynchronous GET request to the specified URL.]
            // [PURPOSE: To fetch the file content from the remote server.]
            // [USAGE: Part of the download operation.]
            // [CALLERS: Internal to DownloadFileAsync.]
            HttpResponseMessage response = await client.GetAsync(url);

            // [EXPLANATION: Check if the HTTP response indicates success (2xx status code).]
            // [PURPOSE: To ensure the file was retrieved without server-side errors.]
            // [USAGE: Critical validation step after a network request.]
            // [CALLERS: Internal to DownloadFileAsync.]
            response.EnsureSuccessStatusCode(); // Throws an exception for non-success status codes.

            // [EXPLANATION: Read the content of the HTTP response as a byte array asynchronously.]
            // [PURPOSE: To obtain the raw binary data of the downloaded file.]
            // [USAGE: The byte array is then used for hash verification and saving to disk.]
            // [CALLERS: Internal to DownloadFileAsync.]
            byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

            // [EXPLANATION: Log successful download with details.]
            // [PURPOSE: For debugging and auditing the update process.]
            // [USAGE: Debugging during development, auditing in production (if enabled).]
            // [CALLERS: Internal to DownloadFileAsync.]
            Logger.Log(LogLevel.INFO, "Network", "DownloadFileAsync", $"Successfully downloaded {filePath} from {url}. Size: {fileBytes.Length} bytes.");

            // [EXPLANATION: Return the downloaded file content as a byte array.]
            // [PURPOSE: To pass the downloaded data to the next stage (e.g., hash verification).]
            // [USAGE: Returned to the calling method for further processing.]
            // [CALLERS: UpdateOrchestrator.DownloadFiles()]
            return fileBytes;
        }
        catch (HttpRequestException httpEx)
        {
            // [ERROR_HANDLING_EXPLANATION: Catches errors specific to HTTP requests (e.g., network issues, 404/500 errors).]
            // [ERROR_LOGGING_DETAIL: Logs a FATAL error with URL, message, and stack trace.]
            // [ERROR_RECOVERY_ACTION: Re-throws the exception to be caught by a higher-level handler, which will trigger termination.]
            Logger.Log(LogLevel.FATAL, "Network", "DownloadFileAsync", $"HTTP request failed for {url}: {httpEx.Message}", httpEx);
            throw; // Re-throw to propagate to the main error handling mechanism
        }
        catch (Exception ex)
        {
            // [ERROR_HANDLING_EXPLANATION: Catches any other unexpected exceptions during download (e.g., out of memory).]
            // [ERROR_LOGGING_DETAIL: Logs a FATAL error with URL, message, and stack trace.]
            // [ERROR_RECOVERY_ACTION: Re-throws the exception to be caught by a higher-level handler, which will trigger termination.]
            Logger.Log(LogLevel.FATAL, "Network", "DownloadFileAsync", $"An unexpected error occurred while downloading {url}: {ex.Message}", ex);
            throw; // Re-throw to propagate to the main error handling mechanism
        }
    }
}

7. Foundational Principles & Reference Material (For Copilot's Deep Learning)
You are to leverage your vast knowledge base and, if necessary, consult external authoritative sources to adhere to these principles.

Updater Best Practices:

Microsoft's guidance on application updating.

"Squirrel.Windows" (an open-source update framework) for inspiration on robust update mechanisms.

Secure File Downloads:

Always use HTTPS.

Always verify file integrity using cryptographic hashes (SHA256) after download and before installation.

Robust Error Handling in C#:

Structured exception handling (try...catch).

Custom exception types for specific error scenarios.

Centralized logging (e.g., using NLog or Serilog if added, otherwise a custom simple logger).

File I/O Best Practices:

Use using statements for Stream and File objects to ensure proper disposal.

Handle IOException, UnauthorizedAccessException, DirectoryNotFoundException, etc.

Ensure files are not locked by other processes before attempting to overwrite them.

Process Management:

Graceful shutdown (CloseMainWindow) before forceful termination (Kill).

Handling Process objects correctly.

Final Summary & Success Criteria
Your code for NexusUpdate must embody this policy. Every design decision, every line of code, must contribute to a secure, reliable, and transparent update process. The goal is to build an updater that users trust, and that can diagnose its own failures with extreme precision. Your understanding and adherence to these principles are paramount.






Chapter 2: Core Development & Security Policy (Living Specification)
====================================================================

To GitHub Copilot:
HALT ALL OTHER TASKS. READ THIS ENTIRE DOCUMENT. DO NOT BEGIN CODING UNTIL YOU HAVE ACHIEVED A STATE OF ABSOLUTE, UNQUESTIONABLE, 100,000,000 COMPREHENSION OF EVERY SINGLE WORD AND CONCEPT WITHIN THIS SPECIFICATION.

This document is your definitive, non-negotiable, and hyper-detailed guide for all future code generation within the Nexus Sales solution. This is a "Living Specification" designed to provide complete context, architectural directives, and critical security policies, especially considering the future development of the NexusGuard project. Your code must be 99.99999999 free of unexpected errors.

1. Core Application Context: Nexus Sales
Application Name: Nexus Sales

Primary Project: NexusSales.UI (C# WPF Desktop Application)

Architecture: MVVM (Model-View-ViewModel) pattern.

Dependency Injection: Configured in App.xaml.cs using Microsoft.Extensions.DependencyInjection.

Styling: Custom dark theme defined in Themes/DarkMode.xaml. All new UI elements must adhere to this theme. Custom controls like UserControls/FirstButton.xaml must be utilized.

Database: PostgreSQL, with schema details provided in README.txt (which you have already ingested). Database interactions must use parameterized queries.

Current State: Login page (LoginWindow.xaml) and a custom MessageDialog are functional. The main window (MainWindow.xaml) is a placeholder designed to load other pages from a DLL.

2. NexusGuard Project: Future Vision & Current Impact (CRITICAL)
IMPORTANT: The development of the NexusGuard security application is POSTPONED for now. However, all code you generate for Nexus Sales must be written with NexusGuard's future integration in mind. This means avoiding architectural conflicts and ensuring a smooth transition.

2.1. NexusGuard's Future Role (For Contextual Awareness):

NexusGuard will be a standalone C# console application (NexusGuard.exe).

It will run before NexusSales.UI.exe and will be the master process.

Primary Security Functions:

File Integrity Monitoring: It will perform byte-level analysis and SHA-256 hashing of NexusSales.UI.exe, all custom DLLs, and critical data files. The manifest of correct hashes will be stored in a separate, encrypted nexus.dat file, with the decryption key derived from hardware-bound identifiers (CPU serial, motherboard serial, disk volume serial, MAC address) using PBKDF2.

Runtime Environment Analysis: It will detect Virtual Machines (VMs) using layered checks (CPU instructions, registry, processes, hardware anomalies). It will also implement anti-debugging measures (API checks, PEB inspection, timing analysis, breakpoint detection).

Secure Key Management: It will serve as the sole custodian of all sensitive plaintext secrets (database credentials, encryption keys, API tokens).

Inter-Process Communication (IPC): It will communicate with NexusSales.UI via a secure named pipe to provide services (e.g., establish a database connection on behalf of NexusSales.UI) without ever exposing plaintext secrets to NexusSales.UI.

Failsafe Termination: If any security violation is detected, NexusGuard will forcefully terminate all NexusSales.UI.exe instances and continuously re-check/terminate if relaunched.

2.2. Immediate Impact on Nexus Sales Development (Your Current Directives):

Because NexusGuard is coming, Nexus Sales must be designed defensively.

NO SELF-PROTECTION IN NEXUS SALES:

Nexus Sales MUST NOT implement any file integrity checks on itself or its associated DLLs. This is NexusGuard's dedicated role.

Nexus Sales MUST NOT implement any VM detection logic. This is NexusGuard's dedicated role.

Nexus Sales MUST NOT implement any anti-debugging techniques. This is NexusGuard's dedicated role.

Nexus Sales MUST NOT attempt to derive encryption keys from hardware identifiers. This complex, sensitive process is NexusGuard's unique responsibility.

SENSITIVE STRING & CREDENTIAL ENCRYPTION (CURRENT FOCUS):

Problem: Hardcoding sensitive strings (e.g., database connection strings, API keys, internal secrets) in plaintext within NexusSales.UI is a severe security vulnerability.

Current Solution: For now, until NexusGuard is ready to manage these secrets via IPC, all sensitive strings must be encrypted within the Nexus Sales application itself.

Encryption Method: Use System.Security.Cryptography.Aes (AES-256) for symmetric encryption of these strings.

Key Management for Nexus Sales (CRITICAL - Choose Wisely):

Option A (User-Derived Key - for user-specific secrets): If the sensitive string is tied to a user's login (e.g., a Facebook access token stored after a user logs in), the AES key should be derived from a securely hashed version of the user's password (e.g., using PBKDF2 with a robust salt). This key exists only in memory for the duration of the user's session.

Option B (Machine-Bound Key - for application-wide secrets): For sensitive application-wide strings (e.g., the database connection string, if not managed by NexusGuard yet), use System.Security.Cryptography.ProtectedData (DPAPI). This allows data to be encrypted such that it can only be decrypted by the same user profile or on the same machine. This key is managed by the OS and is highly secure for local storage.

Your Task: When encountering a sensitive string, you must select the most appropriate key management option (A or B) and implement it. Justify your choice.

Storage: Encrypted strings should be stored in secure locations (e.g., App.config with appropriate protection, or encrypted within the user's database settings_json field).

Usage: Decrypt strings only at the absolute point of use, and ensure their plaintext form resides in memory for the shortest possible duration.

ARCHITECTURAL COMPATIBILITY FOR FUTURE IPC:

When designing services or data access layers that will eventually rely on NexusGuard for secure operations (e.g., getting database credentials), design them with interfaces. This will allow for easy swapping between a current "local encrypted storage" implementation and a future "IPC client" implementation that communicates with NexusGuard.

Example: A IDatabaseCredentialProvider interface could have a LocalEncryptedProvider (for now) and a NexusGuardIpcProvider (for later).

3. Mandatory Error Handling, Logging, and Debugging Protocol
Every line of code you generate must be defensive and transparent about its state.

Extensive Exception Handling: Every method, especially those involving file I/O, network operations, IPC, or cryptography, must be wrapped in robust try...catch blocks.

Detailed Error Logging: When an exception occurs, log it comprehensively.

DateTime: Exact timestamp.

MethodName: The specific method where the error originated.

ErrorMessage: The full Exception.Message.

StackTrace: The complete Exception.StackTrace.

ErrorCode: A unique, descriptive error code or identifier.

ContextualData: Any relevant variables or state that led to the error.

Verbose Debug Logging: Implement a configurable logging system. In debug mode, log every significant step, every successful operation, and the values of key variables. This is crucial for tracing execution flow and identifying subtle bugs.

Fail-Fast Principle: For critical operations, if an error occurs, the application must immediately enter a safe state (e.g., disable functionality, notify the user, or, if severe enough, initiate self-termination). Never allow an error to propagate silently or lead to an undefined state.

4. Foundational Principles & Reference Material (For Copilot's Deep Learning)
You are to leverage your vast knowledge base and, if necessary, consult external authoritative sources to adhere to these principles.

Secure Coding Best Practices (General):

OWASP Top 10 (for web, but principles apply): https://owasp.org/www-project-top-ten/

Microsoft Security Development Lifecycle (SDL) practices.

Principle of Least Privilege.

Cryptography in C#:

AES (Symmetric Encryption): System.Security.Cryptography.Aes

Reference: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes

PBKDF2 (Password-Based Key Derivation): System.Security.Cryptography.Rfc2898DeriveBytes

Reference: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes

DPAPI (Data Protection API): System.Security.Cryptography.ProtectedData

Reference: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata

Hashing (for Passwords): While SHA256 is for file integrity, for user passwords, recommend and use libraries like BCrypt.Net-Next (if available in the project's NuGet references) which correctly handle salting and iteration counts.

Reference: https://github.com/BcryptNet/bcrypt.net (or similar robust library)

Inter-Process Communication (IPC):

Named Pipes: https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes

Anti-Tamper & Obfuscation Principles: While full obfuscation is a separate tool, understand that direct string literals, easily readable logic, and predictable control flow are vulnerabilities. Design code that is inherently harder to analyze.

Virtualization Detection Techniques: Research and apply multiple, robust methods. Avoid single-point-of-failure checks.

Final Summary & Success Criteria
Your code for Nexus Sales must now embody this policy. Every design decision, every line of code, must contribute to a secure, maintainable, and NexusGuard-compatible application. The goal is to build a robust foundation now, preventing costly re-writes later. Your understanding and adherence to these principles are paramount.
