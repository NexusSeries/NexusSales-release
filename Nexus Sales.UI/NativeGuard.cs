using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using NexusSales.UI.Dialogs;

public static class NativeGuard
{
    [DllImport("guard.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
    private static extern IntPtr DecryptString([MarshalAs(UnmanagedType.LPWStr)] string encryptedBase64String);

    public static string Decrypt(string encryptedBase64)
    {
        string decryptedString = null;
        IntPtr unmanagedPointer = IntPtr.Zero;

        try
        {
            unmanagedPointer = DecryptString(encryptedBase64);

            if (unmanagedPointer != IntPtr.Zero)
            {
                decryptedString = Marshal.PtrToStringUni(unmanagedPointer);
            }
        }
        finally
        {
            if (unmanagedPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(unmanagedPointer);
            }
        }

        return decryptedString;
    }
}

// Example usage
public static class ApplicationStrings
{
    private static readonly string encryptedWelcomeMessage = "DCkmQAkFADoNHl5aFQZAIys0IQkBXVxrVydFUBQcGh4heyVbRx0IKwkBDWUHAQgEKzQxCXIIDDwOHVleV0RCQ3dzbQB5CAE4CxdEA1RGSURwd20PcAgbOA4TRVBbAhQAMCEnUUdS";

    public static void DisplayWelcomeMessage()
    {
        string finalDecryptedMessage = NativeGuard.Decrypt(encryptedWelcomeMessage);

        if (!string.IsNullOrEmpty(finalDecryptedMessage))
        {
            var dialog = new MessageDialog(
                finalDecryptedMessage,
                "Welcome",
                soundFileName: "Success.wav",
                titleColor: (Brush)System.Windows.Application.Current.FindResource("FontSuccessfulBrush")
            );
            dialog.ShowDialog();
        }
        else
        {
            var dialog = new MessageDialog(
                "Decryption failed.",
                "Decryption Error",
                soundFileName: "Warning.wav",
                titleColor: (Brush)System.Windows.Application.Current.FindResource("FontWarningBrush")
            );
            dialog.ShowDialog();
        }
    }
}
