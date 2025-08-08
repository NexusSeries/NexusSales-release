Chapter 3: Core Development & Security Policy (Living Specification)
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