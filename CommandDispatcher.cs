using System;
using System.Collections.Generic;
using System.Configuration; // For App.config access
using System.Reflection;    // For dynamic method invocation
using System.Linq;          // For attribute parsing
using System.Globalization; // For culture-invariant parsing
using NexusSales.Core.Commanding.Handlers;

namespace NexusSales.Core.Commanding
{
    /// <summary>
    /// Represents a parsed command from the FrontEnd.
    /// Example: [Facebook][Post][ReadComments, PostID]
    /// </summary>
    public class CommandRequest
    {
        // The target application/platform (e.g., "Facebook", "Messenger")
        public string App { get; set; }
        // The function group or section (e.g., "Post", "Send")
        public string Section { get; set; }
        // The specific action or function (e.g., "ReadComments", "ExtractId")
        public string Action { get; set; }
        // Optional attributes/parameters (e.g., PostID, Message, UserID)
        public List<string> Attributes { get; set; } = new List<string>();
    }

    /// <summary>
    /// The result of executing a command.
    /// </summary>
    public class CommandResult
    {
        // Indicates if the command was successful
        public bool Success { get; set; }
        // The output or error message
        public string Output { get; set; }
        // Optional: Any data returned by the command
        public object Data { get; set; }
    }

    /// <summary>
    /// Central dispatcher for all commands sent from the FrontEnd.
    /// </summary>
    public static class CommandDispatcher
    {
        // Centralized logger for structured, secure logging
        private static void Log(string level, string method, string message, Exception ex = null, string errorCode = null, object context = null)
        {
            // Never log sensitive data (passwords, tokens, connection strings, raw command strings)
            string logLine = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{level}] CommandDispatcher.{method} {message}";
            if (!string.IsNullOrEmpty(errorCode)) logLine += $" [ErrorCode: {errorCode}]";
            if (context != null) logLine += $" [Context: {Newtonsoft.Json.JsonConvert.SerializeObject(context)}]";
            if (ex != null) logLine += $"\nException: {ex.Message}\nStackTrace: {ex.StackTrace}";
            System.Diagnostics.Debug.WriteLine(logLine);
        }

        /// <summary>
        /// Entry point: Receives a command string, parses it, and executes the mapped function.
        /// </summary>
        /// <param name="commandString">The command in format: [App][Section][Action, Attr1, Attr2...]</param>
        /// <returns>CommandResult with output or error</returns>
        public static CommandResult Execute(string commandString)
        {
            try
            {
                // [EXPLANATION: Parses the input command string into a structured CommandRequest object.]
                // [PURPOSE: To break down the command into its component parts (App, Section, Action, Attributes).]
                // [HOW_IT_WORKS: Uses the ParseCommand method to split and validate the command structure.]
                // [USAGE: Essential first step in command processing to enable routing and validation.]
                // [CALLERS: Internal to Execute method.]
                var request = ParseCommand(commandString);

                // [EXPLANATION: Constructs the configuration key for function name lookup.]
                // [PURPOSE: To create a standardized key format for App.config function mapping.]
                // [HOW_IT_WORKS: Concatenates App, Section, and Action with dots as separators.]
                // [USAGE: Used to retrieve the actual method name from encrypted configuration.]
                // [CALLERS: Internal to Execute method.]
                string functionKey = $"{request.App}.{request.Section}.{request.Action}";
                
                // [EXPLANATION: Retrieves the actual function name from the encrypted App.config settings.]
                // [PURPOSE: To map the command action to the actual C# method name for reflection.]
                // [HOW_IT_WORKS: Uses ConfigurationManager to access the appSettings section.]
                // [USAGE: Critical mapping step that enables dynamic method invocation.]
                // [CALLERS: Internal to Execute method.]
                string functionName = ConfigurationManager.AppSettings[functionKey];

                // [EXPLANATION: Validates that a function mapping was found in the configuration.]
                // [PURPOSE: To prevent execution of unmapped or unauthorized commands.]
                // [USAGE: Security and validation check to ensure only configured commands can execute.]
                // [CALLERS: Internal to Execute method.]
                if (string.IsNullOrWhiteSpace(functionName))
                {
                    // [EXPLANATION: Returns a failed CommandResult when no function mapping is found.]
                    // [PURPOSE: To inform the caller that the requested command is not configured.]
                    // [USAGE: Error response for unmapped or disabled commands.]
                    // [CALLERS: Calling code that invokes CommandDispatcher.Execute().]
                    Log("WARNING", "Execute", $"Function mapping for '{functionKey}' not found in configuration.", null, "CMD-NOMAP-001", new { functionKey });
                    return new CommandResult
                    {
                        Success = false,
                        Output = $"Function mapping for '{functionKey}' not found in configuration."
                    };
                }

                // [EXPLANATION: Dynamically resolves the handler class type using reflection.]
                // [PURPOSE: To locate the static class that contains the command implementation.]
                // [HOW_IT_WORKS: Uses Type.GetType with a standardized naming convention.]
                // [USAGE: Enables dynamic dispatch to the appropriate command handler.]
                // [CALLERS: Internal to Execute method.]
                var handlerType = Type.GetType($"NexusSales.Core.Commanding.Handlers.{request.App}Handler");
                
                // [EXPLANATION: Validates that the handler class was successfully located.]
                // [PURPOSE: To ensure the required command handler implementation exists.]
                // [USAGE: Prevents execution when handler classes are missing or incorrectly named.]
                // [CALLERS: Internal to Execute method.]
                if (handlerType == null)
                {
                    // [EXPLANATION: Returns a failed CommandResult when the handler class is not found.]
                    // [PURPOSE: To inform the caller that the command handler is not implemented.]
                    // [USAGE: Error response for missing handler implementations.]
                    // [CALLERS: Calling code that invokes CommandDispatcher.Execute().]
                    Log("ERROR", "Execute", $"Handler for '{request.App}' not implemented.", null, "CMD-NOHANDLER-002", new { app = request.App });
                    return new CommandResult
                    {
                        Success = false,
                        Output = $"Handler for '{request.App}' not implemented."
                    };
                }

                // [EXPLANATION: Locates the specific method within the handler class using reflection.]
                // [PURPOSE: To find the exact method that should be invoked for this command.]
                // [HOW_IT_WORKS: Uses GetMethod with case-insensitive search and static binding flags.]
                // [USAGE: Final step in method resolution before parameter preparation.]
                // [CALLERS: Internal to Execute method.]
                var method = handlerType.GetMethod(functionName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                
                // [EXPLANATION: Validates that the specific method was found in the handler class.]
                // [PURPOSE: To ensure the method exists and can be invoked.]
                // [USAGE: Prevents execution when methods are missing or incorrectly named.]
                // [CALLERS: Internal to Execute method.]
                if (method == null)
                {
                    // [EXPLANATION: Returns a failed CommandResult when the method is not found.]
                    // [PURPOSE: To inform the caller that the specific method is not implemented.]
                    // [USAGE: Error response for missing method implementations.]
                    // [CALLERS: Calling code that invokes CommandDispatcher.Execute().]
                    Log("ERROR", "Execute", $"Function '{functionName}' not found in handler '{handlerType.Name}'.", null, "CMD-NOMETHOD-003", new { functionName, handler = handlerType.Name });
                    return new CommandResult
                    {
                        Success = false,
                        Output = $"Function '{functionName}' not found in handler '{handlerType.Name}'."
                    };
                }

                // [EXPLANATION: Retrieves parameter metadata from the target method for argument preparation.]
                // [PURPOSE: To understand what parameters the method expects and in what order.]
                // [HOW_IT_WORKS: Uses reflection to get parameter information including names and types.]
                // [USAGE: Essential for proper parameter mapping and type conversion.]
                // [CALLERS: Internal to Execute method.]
                var parameters = method.GetParameters();
                
                // [EXPLANATION: Creates an array to hold the prepared method arguments.]
                // [PURPOSE: To store the converted and mapped parameters for method invocation.]
                // [USAGE: Populated in the following loop and passed to method.Invoke.]
                // [CALLERS: Internal to Execute method.]
                object[] args = new object[parameters.Length];
                
                // [EXPLANATION: Iterates through each method parameter to prepare the corresponding argument.]
                // [PURPOSE: To map command attributes to the correct method parameters.]
                // [HOW_IT_WORKS: Uses parameter index to populate the args array with converted values.]
                // [USAGE: Critical parameter preparation step that enables proper method invocation.]
                // [CALLERS: Internal to Execute method.]
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i < request.Attributes.Count)
                    {
                        try
                        {
                            if (parameters[i].ParameterType == typeof(string))
                                args[i] = request.Attributes[i];
                            else
                                args[i] = Convert.ChangeType(request.Attributes[i], parameters[i].ParameterType, CultureInfo.InvariantCulture);
                        }
                        catch (FormatException ex)
                        {
                            Log("ERROR", "Execute", $"Failed to convert '{request.Attributes[i]}' to {parameters[i].ParameterType.Name}", ex);
                            throw new FormatException($"Failed to convert '{request.Attributes[i]}' to {parameters[i].ParameterType.Name}", ex);
                        }
                    }
                    else if (parameters[i].IsOptional)
                    {
                        args[i] = parameters[i].DefaultValue;
                    }
                    else
                    {
                        Log("ERROR", "Execute", $"Missing required parameter: {parameters[i].Name}", null, "CMD-MISSINGPARAM-004", new { param = parameters[i].Name });
                        throw new ArgumentException($"Missing parameter does not have a default value. Parameter name: {parameters[i].Name}");
                    }
                }

                // [EXPLANATION: Invokes the target method with the prepared arguments using reflection.]
                // [PURPOSE: To execute the actual command logic with the properly mapped parameters.]
                // [HOW_IT_WORKS: Uses method.Invoke with null target (static method) and prepared arguments.]
                // [USAGE: Core execution step that runs the requested command functionality.]
                // [CALLERS: Internal to Execute method.]
                var result = method.Invoke(null, args);
                
                // [EXPLANATION: Returns a successful CommandResult with the method's return value.]
                // [PURPOSE: To provide the command execution result back to the calling code.]
                // [HOW_IT_WORKS: Wraps the method result in a structured CommandResult object.]
                // [USAGE: Success response containing the command output.]
                // [CALLERS: Calling code that invokes CommandDispatcher.Execute().]
                Log("INFO", "Execute", $"Command executed successfully for '{functionKey}'.", null, null, new { functionKey });
                return new CommandResult
                {
                    Success = true,
                    Output = result?.ToString(),
                    Data = result
                };
            }
            catch (Exception ex)
            {
                // [ERROR_HANDLING_EXPLANATION: Catches any exceptions during command parsing, validation, or execution.]
                // [ERROR_LOGGING_DETAIL: Logs the complete exception information for debugging and troubleshooting.]
                // [ERROR_CODE: CMD-EXEC-GEN-001]
                // [CONTEXTUAL_DATA: { "Command": commandString, "Exception": ex.Message, "StackTrace": ex.StackTrace }]
                Log("FATAL", "Execute", "Exception during command execution.", ex, "CMD-EXEC-GEN-001", new { error = ex.Message });
                return new CommandResult
                {
                    Success = false,
                    Output = $"Error executing command: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Overloaded entry point: Receives a command string and tokens, parses it, and executes the mapped function.
        /// </summary>
        /// <param name="commandString">The command in format: [App][Section][Action, Attr1, Attr2...]</param>
        /// <param name="tokens">A dictionary of tokens for additional context</param>
        /// <returns>CommandResult with output or error</returns>
        public static CommandResult Execute(string commandString, Dictionary<string, string> tokens)
        {
            try
            {
                // [EXPLANATION: Parses the input command string into a structured CommandRequest object.]
                // [PURPOSE: To break down the command into its component parts (App, Section, Action, Attributes).]
                // [HOW_IT_WORKS: Uses the ParseCommand method to split and validate the command structure.]
                // [USAGE: Essential first step in command processing to enable routing and validation.]
                // [CALLERS: Internal to Execute method.]
                var request = ParseCommand(commandString);

                // [EXPLANATION: Validates Facebook-specific tokens when the command targets the Facebook platform.]
                // [PURPOSE: To ensure required authentication tokens are present and valid before execution.]
                // [HOW_IT_WORKS: Uses FacebookTokenValidator to check for required token presence and format.]
                // [USAGE: Security validation step that prevents unauthorized or malformed requests.]
                // [CALLERS: Internal to Execute method.]
                if (request.App.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
                {
                    // [EXPLANATION: Creates a new FacebookTokenValidator instance for token validation.]
                    // [PURPOSE: To utilize the specialized validation logic for Facebook authentication tokens.]
                    // [USAGE: Part of the security validation pipeline for Facebook commands.]
                    // [CALLERS: Internal to Execute method.]
                    var validator = new FacebookTokenValidator();

                    // [EXPLANATION: Performs the actual token validation using the validator instance.]
                    // [PURPOSE: To ensure all required tokens are present and properly formatted.]
                    // [HOW_IT_WORKS: Returns false if validation fails and populates error message.]
                    // [USAGE: Critical security check before allowing Facebook API operations.]
                    // [CALLERS: Internal to Execute method.]
                    if (!validator.ValidateTokens(tokens, out string error))
                    {
                        // [EXPLANATION: Returns a failed CommandResult when token validation fails.]
                        // [PURPOSE: To immediately stop execution and inform the caller of authentication issues.]
                        // [USAGE: Error response for invalid or missing authentication tokens.]
                        // [CALLERS: ExtractDataView.xaml.cs ExtractFacebookPostId_Click method.]
                        Log("WARNING", "Execute", $"Token validation failed: {error}", null, "CMD-TOKEN-005", new { app = request.App });
                        return new CommandResult
                        {
                            Success = false,
                            Output = $"Token validation failed: {error}"
                        };
                    }
                }

                // [EXPLANATION: Constructs the configuration key for function name lookup.]
                // [PURPOSE: To create a standardized key format for App.config function mapping.]
                // [HOW_IT_WORKS: Concatenates App, Section, and Action with dots as separators.]
                // [USAGE: Used to retrieve the actual method name from encrypted configuration.]
                // [CALLERS: Internal to Execute method.]
                string functionKey = $"{request.App}.{request.Section}.{request.Action}";

                // [EXPLANATION: Retrieves the actual function name from the encrypted App.config settings.]
                // [PURPOSE: To map the command action to the actual C# method name for reflection.]
                // [HOW_IT_WORKS: Uses ConfigurationManager to access the appSettings section.]
                // [USAGE: Critical mapping step that enables dynamic method invocation.]
                // [CALLERS: Internal to Execute method.]
                string functionName = ConfigurationManager.AppSettings[functionKey];

                // [EXPLANATION: Validates that a function mapping was found in the configuration.]
                // [PURPOSE: To prevent execution of unmapped or unauthorized commands.]
                // [USAGE: Security and validation check to ensure only configured commands can execute.]
                // [CALLERS: Internal to Execute method.]
                if (string.IsNullOrWhiteSpace(functionName))
                {
                    // [EXPLANATION: Returns a failed CommandResult when no function mapping is found.]
                    // [PURPOSE: To inform the caller that the requested command is not configured.]
                    // [USAGE: Error response for unmapped or disabled commands.]
                    // [CALLERS: ExtractDataView.xaml.cs ExtractFacebookPostId_Click method.]
                    Log("WARNING", "Execute", $"Function mapping for '{functionKey}' not found in configuration.", null, "CMD-NOMAP-001", new { functionKey });
                    return new CommandResult
                    {
                        Success = false,
                        Output = $"Function mapping for '{functionKey}' not found in configuration."
                    };
                }

                // [EXPLANATION: Dynamically resolves the handler class type using reflection.]
                // [PURPOSE: To locate the static class that contains the command implementation.]
                // [HOW_IT_WORKS: Uses Type.GetType with a standardized naming convention.]
                // [USAGE: Enables dynamic dispatch to the appropriate command handler.]
                // [CALLERS: Internal to Execute method.]
                var handlerType = Type.GetType($"NexusSales.Core.Commanding.Handlers.{request.App}Handler");

                // [EXPLANATION: Validates that the handler class was successfully located.]
                // [PURPOSE: To ensure the required command handler implementation exists.]
                // [USAGE: Prevents execution when handler classes are missing or incorrectly named.]
                // [CALLERS: Internal to Execute method.]
                if (handlerType == null)
                {
                    // [EXPLANATION: Returns a failed CommandResult when the handler class is not found.]
                    // [PURPOSE: To inform the caller that the command handler is not implemented.]
                    // [USAGE: Error response for missing handler implementations.]
                    // [CALLERS: ExtractDataView.xaml.cs ExtractFacebookPostId_Click method.]
                    Log("ERROR", "Execute", $"Handler for '{request.App}' not implemented.", null, "CMD-NOHANDLER-002", new { app = request.App });
                    return new CommandResult
                    {
                        Success = false,
                        Output = $"Handler for '{request.App}' not implemented."
                    };
                }

                // [EXPLANATION: Locates the specific method within the handler class using reflection.]
                // [PURPOSE: To find the exact method that should be invoked for this command.]
                // [HOW_IT_WORKS: Uses GetMethod with case-insensitive search and static binding flags.]
                // [USAGE: Final step in method resolution before parameter preparation.]
                // [CALLERS: Internal to Execute method.]
                var method = handlerType.GetMethod(functionName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

                // [EXPLANATION: Validates that the specific method was found in the handler class.]
                // [PURPOSE: To ensure the method exists and can be invoked.]
                // [USAGE: Prevents execution when methods are missing or incorrectly named.]
                // [CALLERS: Internal to Execute method.]
                if (method == null)
                {
                    // [EXPLANATION: Returns a failed CommandResult when the method is not found.]
                    // [PURPOSE: To inform the caller that the specific method is not implemented.]
                    // [USAGE: Error response for missing method implementations.]
                    // [CALLERS: ExtractDataView.xaml.cs ExtractFacebookPostId_Click method.]
                    Log("ERROR", "Execute", $"Function '{functionName}' not found in handler '{handlerType.Name}'.", null, "CMD-NOMETHOD-003", new { functionName, handler = handlerType.Name });
                    return new CommandResult
                    {
                        Success = false,
                        Output = $"Function '{functionName}' not found in handler '{handlerType.Name}'."
                    };
                }

                // [EXPLANATION: Retrieves parameter metadata from the target method for argument preparation.]
                // [PURPOSE: To understand what parameters the method expects and in what order.]
                // [HOW_IT_WORKS: Uses reflection to get parameter information including names and types.]
                // [USAGE: Essential for proper parameter mapping and type conversion.]
                // [CALLERS: Internal to Execute method.]
                var parameters = method.GetParameters();

                // [EXPLANATION: Creates an array to hold the prepared method arguments.]
                // [PURPOSE: To store the converted and mapped parameters for method invocation.]
                // [USAGE: Populated in the following loop and passed to method.Invoke.]
                // [CALLERS: Internal to Execute method.]
                object[] args = new object[parameters.Length];

                // [EXPLANATION: Iterates through each method parameter to prepare the corresponding argument.]
                // [PURPOSE: To map command attributes and tokens to the correct method parameters.]
                // [HOW_IT_WORKS: Uses parameter index and name matching to populate the args array.]
                // [USAGE: Critical parameter preparation step that enables proper method invocation.]
                // [CALLERS: Internal to Execute method.]
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i < request.Attributes.Count)
                    {
                        try
                        {
                            if (parameters[i].ParameterType == typeof(string))
                                args[i] = request.Attributes[i];
                            else
                                args[i] = Convert.ChangeType(request.Attributes[i], parameters[i].ParameterType, CultureInfo.InvariantCulture);
                        }
                        catch (FormatException ex)
                        {
                            Log("ERROR", "Execute", $"Failed to convert '{request.Attributes[i]}' to {parameters[i].ParameterType.Name}", ex);
                            throw new FormatException($"Failed to convert '{request.Attributes[i]}' to {parameters[i].ParameterType.Name}", ex);
                        }
                    }
                    else if (parameters[i].Name.Equals("cUserCookie", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = tokens.ContainsKey("c_user_token") ? tokens["c_user_token"] : null;
                    }
                    else if (parameters[i].Name.Equals("xsCookie", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = tokens.ContainsKey("xs_token") ? tokens["xs_token"] : null;
                    }
                    else if (parameters[i].Name.Equals("reactionType", StringComparison.OrdinalIgnoreCase))
                    {
                        // Default to Like (1) if not provided
                        if (tokens.ContainsKey("reactionType"))
                            args[i] = Convert.ChangeType(tokens["reactionType"], parameters[i].ParameterType, CultureInfo.InvariantCulture);
                        else
                            args[i] = 1;
                    }
                    else if (parameters[i].IsOptional)
                    {
                        args[i] = parameters[i].DefaultValue;
                    }
                    else
                    {
                        Log("ERROR", "Execute", $"Missing required parameter: {parameters[i].Name}", null, "CMD-MISSINGPARAM-004", new { param = parameters[i].Name });
                        throw new ArgumentException($"Missing parameter does not have a default value. Parameter name: {parameters[i].Name}");
                    }
                }

                // [EXPLANATION: Invokes the target method with the prepared arguments using reflection.]
                // [PURPOSE: To execute the actual command logic with the properly mapped parameters.]
                // [HOW_IT_WORKS: Uses method.Invoke with null target (static method) and prepared arguments.]
                // [USAGE: Core execution step that runs the requested command functionality.]
                // [CALLERS: Internal to Execute method.]
                var result = method.Invoke(null, args);

                // [EXPLANATION: Returns a successful CommandResult with the method's return value.]
                // [PURPOSE: To provide the command execution result back to the calling code.]
                // [HOW_IT_WORKS: Wraps the method result in a structured CommandResult object.]
                // [USAGE: Success response containing the command output.]
                // [CALLERS: ExtractDataView.xaml.cs ExtractFacebookPostId_Click method.]
                Log("INFO", "Execute", $"Command executed successfully for '{functionKey}'.", null, null, new { functionKey });
                return new CommandResult
                {
                    Success = true,
                    Output = result?.ToString(),
                    Data = result
                };
            }
            catch (Exception ex)
            {
                // [ERROR_HANDLING_EXPLANATION: Catches any exceptions during command parsing, validation, or execution.]
                // [ERROR_LOGGING_DETAIL: Logs the complete exception information for debugging and troubleshooting.]
                // [ERROR_CODE: CMD-EXEC-GEN-002]
                // [CONTEXTUAL_DATA: { "Command": commandString, "Exception": ex.Message, "StackTrace": ex.StackTrace }]
                Log("FATAL", "Execute", "Exception during command execution.", ex, "CMD-EXEC-GEN-002", new { error = ex.Message });
                return new CommandResult
                {
                    Success = false,
                    Output = $"Error executing command: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Parses a command string into a CommandRequest object.
        /// Example: [Facebook][Post][ReadComments, 123456789]
        /// </summary>
        private static CommandRequest ParseCommand(string command)
        {
            // [EXPLANATION: Splits the command string by square brackets and removes empty entries.]
            // [PURPOSE: To extract the individual components (App, Section, Action+Attributes) from the command format.]
            // [HOW_IT_WORKS: Uses string.Split with bracket delimiters and trims whitespace from each part.]
            // [USAGE: Core parsing logic that converts structured command strings into manageable pieces.]
            // [CALLERS: Execute() methods within CommandDispatcher.]
            var parts = command.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(p => p.Trim()).ToList();

            // [EXPLANATION: Validates that the command has the minimum required number of parts.]
            // [PURPOSE: To ensure the command follows the expected [App][Section][Action,...] format.]
            // [USAGE: Input validation that prevents processing of malformed commands.]
            // [CALLERS: Internal to ParseCommand.]
            if (parts.Count < 3)
                throw new ArgumentException("Invalid command format. Expected at least [App][Section][Action,...]");

            // [EXPLANATION: Splits the action and attributes section by commas and trims whitespace.]
            // [PURPOSE: To separate the action name from its optional parameters/attributes.]
            // [HOW_IT_WORKS: Takes the third part (index 2) and splits by comma, then trims each piece.]
            // [USAGE: Extracts the specific action and any additional parameters for the command.]
            // [CALLERS: Internal to ParseCommand.]
            var actionAndAttrs = parts[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(s => s.Trim()).ToList();

            // [EXPLANATION: Creates and returns a structured CommandRequest object with parsed components.]
            // [PURPOSE: To provide a strongly-typed representation of the command for further processing.]
            // [HOW_IT_WORKS: Maps parsed string components to object properties, with attributes excluding the action name.]
            // [USAGE: Final step in command parsing that produces the object used by the dispatcher.]
            // [CALLERS: Execute() methods within CommandDispatcher.]
            return new CommandRequest
            {
                App = parts[0],
                Section = parts[1],
                Action = actionAndAttrs[0],
                Attributes = actionAndAttrs.Skip(1).ToList()
            };
        }
    }
}

/*
================================================================================
FINAL EXHAUSTIVE SCRIPT SUMMARY FOR CommandDispatcher.cs
================================================================================

Overall Function:
The CommandDispatcher.cs file serves as the central command execution engine for the NexusSales application. It acts as a bridge between the frontend UI components and the backend command handlers, providing a unified, secure, and extensible mechanism for processing structured commands. The dispatcher supports both token-authenticated and non-authenticated command execution, with specialized handling for Facebook API operations.

Execution Flow:
1. A command string (e.g., "[Facebook][Post][ExtractId, https://facebook.com/...]") is received by one of the Execute methods
2. The command is parsed into structured components (App, Section, Action, Attributes) using ParseCommand
3. For Facebook commands, token validation is performed using FacebookTokenValidator
4. The command is mapped to a specific method name via encrypted App.config lookup
5. The target handler class is dynamically resolved using reflection
6. The target method is located within the handler class
7. Method parameters are prepared by mapping command attributes and tokens to parameter positions
8. The method is invoked using reflection with the prepared arguments
9. Results are wrapped in a CommandResult object and returned to the caller

Methods & Functions:
- Execute(string commandString): Single-parameter version for commands without token authentication
- Execute(string commandString, Dictionary<string, string> tokens): Overloaded version with token support for authenticated operations
- ParseCommand(string command): Private helper that converts command strings into CommandRequest objects

Dependencies & Libraries:
- System: Core .NET functionality for basic operations
- System.Collections.Generic: Dictionary and List collections for parameter management
- System.Configuration: ConfigurationManager for encrypted App.config access
- System.Reflection: Dynamic type resolution and method invocation
- System.Linq: Query operations for string processing and collection manipulation
- System.Globalization: CultureInfo for reliable type conversion
- NexusSales.Core.Commanding.Handlers: Contains all command handler implementations (FacebookHandler, etc.)

Data Handling:
Input: Command strings in structured format ([App][Section][Action, Attr1, Attr2, ...]) and optional token dictionaries
Processing: Parses command structure, validates tokens, maps to handler methods, converts parameters, and executes via reflection
Output: CommandResult objects containing success status, output messages, and optional data payloads

Error Management:
The class implements comprehensive error handling with detailed logging for debugging. Each major operation is wrapped in try-catch blocks with specific error codes and contextual information. Token validation failures, missing configuration mappings, handler resolution failures, and method invocation errors are all handled gracefully. All exceptions are logged with full stack traces and relevant context data for troubleshooting.

Future Considerations:
The architecture is designed for extensibility, allowing new command handlers and platforms to be added without modifying the dispatcher core. The encrypted configuration mapping system provides security while maintaining flexibility. The token-based authentication system can be extended to support additional platforms beyond Facebook. Consider implementing command result caching for frequently-executed operations and adding command execution metrics for performance monitoring.

================================================================================
*/
