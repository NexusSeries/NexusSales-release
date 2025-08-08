1. Criteria for Uploading Files to GitHub Repository
a. File Structure and Naming
•	Match the manifest: The files and folder structure in your GitHub repository must exactly match the Path fields in your update_manifest.json.
•	Example: If your manifest has "Path": "Assets/logo.png", then in your repo, the file must be at releases/<version>/Assets/logo.png.
•	Consistent naming: Use consistent, case-sensitive file and folder names.
•	No extra files: Only upload files that are referenced in the manifest.
b. Versioning
•	Organize by version: Place each release’s files in a versioned folder (e.g., releases/1.0.1/).
•	Update the manifest: After uploading new files, update update_manifest.json with the new version, file hashes, sizes, and download URLs.
c. File Integrity
•	SHA256 hash: Compute the SHA256 hash of each file and include it in the manifest.
•	File size: Include the correct file size in bytes in the manifest.
d. Public Access
•	Raw URLs: Files must be accessible via raw GitHub URLs (e.g., https://raw.githubusercontent.com/YourOrg/YourRepo/main/releases/1.0.1/NexusSales.UI.exe).
---
2. Where to Paste the Repository Link
•	Manifest location: The app does not need the repository root, but the direct raw URL to your update_manifest.json.
•	Example:
https://raw.githubusercontent.com/YourOrg/YourRepo/main/releases/update_manifest.json
•	Paste this link in your App.config under the UpdateManifestUrl key:

<appSettings>
  <add key="UpdateManifestUrl" value="https://raw.githubusercontent.com/YourOrg/YourRepo/main/releases/update_manifest.json" />
</appSettings>

3. How the App Knows the File Locations
•	Manifest-driven: The app reads the update_manifest.json from the URL in App.config.
•	DownloadUrl field: Each file entry in the manifest must have a DownloadUrl property pointing to the raw GitHub file.

{
  "Path": "NexusSales.UI.exe",
  "Hash": "a1b2c3...",
  "Size": 1234567,
  "DownloadUrl": "https://raw.githubusercontent.com/YourOrg/YourRepo/main/releases/1.0.1/NexusSales.UI.exe",
  "Description": "Main application executable."
}

4. Example of a Complete Manifest Entry
```json
{
  "Version": "1.0.1",
  "Files": [
	{
	  "Path": "NexusSales.UI.exe",
	  "Hash": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f",
	  "Size": 1234567,
	  "DownloadUrl": "https://raw.githubusercontent.com/YourOrg/YourRepo/main/releases/1.0.1/NexusSales.UI.exe",
	  "Description": "Main application executable."
	},
	{
	  "Path": "Assets/logo.png",
	  "Hash": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e2f",
	  "Size": 2048,
	  "DownloadUrl": "https://raw.githubusercontent.com/YourOrg/YourRepo/main/releases/1.0.1/Assets/logo.png",
	  "Description": "Application logo."
	}
  ]
}
```

5. How to Generate the Manifest
1. Create a JSON file named `update_manifest.json` in your repository's releases folder.
1. Populate it with the version, file paths, hashes, sizes, and download URLs as shown in the example above.
1. Ensure the file structure in your repository matches the paths specified in the manifest.
1. Commit and push the `update_manifest.json` file to your repository.
1. Update the App.config in your application to point to the raw URL of the `update_manifest.json` file.
1. Test the update process to ensure the app can read the manifest and download files correctly.
1. Repeat this process for each new version, updating the manifest with new file entries and version numbers as needed.
1. Ensure that all files referenced in the manifest are uploaded to the correct versioned folder in your repository.
1. Verify that the raw URLs in the manifest are accessible and point to the correct files in your GitHub repository.
1. Maintain the manifest by updating it with each new release, ensuring that all file paths, hashes, sizes, and download URLs are accurate and up-to-date.
1. Consider automating the generation of the manifest using a script that computes file hashes and sizes, ensuring consistency and reducing manual errors.
1. Document the process for future reference, including any scripts or tools used to generate the manifest, to streamline updates and ensure clarity for other developers or maintainers.
1. Keep the manifest and repository organized, ensuring that old versions are archived or removed as necessary to maintain clarity and prevent confusion for users and developers alike.
1. Maintain a changelog or release notes alongside the manifest to provide context for each update, including new features, bug fixes, and any other relevant changes that users should be aware of.
1. Consider implementing a rollback mechanism in your app, allowing users to revert to a previous version if an update causes issues, ensuring a smoother user experience and greater reliability.
1. Regularly back up your repository and manifest to prevent data loss and ensure that you can recover previous versions if needed.
1. 