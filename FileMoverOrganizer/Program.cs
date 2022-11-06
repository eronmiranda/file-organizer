string sourcePath, destinationPath;

Dictionary<string, string> organizedSubPaths = new Dictionary<string, string>();

string[] config = File.ReadAllLines("config.csv");

// Gets the first row of the config file
sourcePath = config[0].Split(',')[1];
// Gets the second row of the config file
destinationPath = config[1].Split(',')[1];

// Gets the extension name and organized sub paths from config.
LoadExtensionSubPaths();

if(Directory.Exists(sourcePath))
    ProcessDirectory(sourcePath);
else
    Console.WriteLine("Chosen directory does not exists! Change the config.csv file.");

// End of main.


// Loads data from config file to dictionary: organizedSubPaths.
// Dictionary < Extension Name, Sub Path > 
void LoadExtensionSubPaths()
{
    // Starts on the 3rd row of the config file.
    // First two rows are for source and destination path.
    for (int i = 2; i < config.Length; i++)
    {
        string[] extensionAndSubPath = config[i].Split(",");
        organizedSubPaths.Add(extensionAndSubPath[0], extensionAndSubPath[1]);
    }
}

// targetDirectory = string path of the source that needs to be moved.
void ProcessDirectory(string targetDirectory)
{
    // Gets all the files from the target/source path.
    string[] fileEntries = Directory.GetFiles(targetDirectory);
    foreach (string fileName in fileEntries)
        ProcessFiles(fileName);

    // Gets all the directories from the target/source path.
    string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
    foreach (string subdirectory in subdirectoryEntries)
        MoveDirectory(subdirectory);
}
// sourceFilePath = string path of source file that needs to be moved.
void ProcessFiles(string sourceFilePath)
{
    string extension = Path.GetExtension(sourceFilePath);
    extension = String.IsNullOrEmpty(extension) ? extension : extension.Substring(1);
    
    if(organizedSubPaths.ContainsKey(extension.ToLower()))
    {
        MoveFile(sourceFilePath, organizedSubPaths[extension.ToLower()], extension);
        Console.WriteLine($"File has been moved to {destinationPath}/{organizedSubPaths[extension.ToLower()]}");
    }
    else
    {
        MoveFile(sourceFilePath, "Others", extension);
        Console.WriteLine($"File has been moved to {destinationPath}/Others");
    }
}

// Added this to prevent files of subdirectory from moving to different organized folders.
void MoveDirectory(string sourceDirectoryPath)
{
    string folderName = Path.GetFileName(sourceDirectoryPath);
    // Creates a path name that stores all directories.
    string subPath = @"Directories\";

    // Combines the destination directory path and sub path.
    string destinationDirectoryPath = Path.Combine(destinationPath, subPath);

    Directory.CreateDirectory(destinationDirectoryPath);

    destinationDirectoryPath = Path.Combine(destinationDirectoryPath, folderName);

    if (Directory.Exists(destinationDirectoryPath))
    {
        destinationDirectoryPath = $"{destinationDirectoryPath}-{DateTime.Now.Date.ToString("ddd")}-{DateTime.Now.Minute}-{DateTime.Now.Second}";
    }

    Directory.Move(sourceDirectoryPath, destinationDirectoryPath);
    Console.WriteLine($@"Directory has been moved to {destinationDirectoryPath}");
    File.AppendAllText("Log.csv", $"{DateTime.Now},{sourceDirectoryPath},{destinationDirectoryPath},{Environment.NewLine}");
}

void MoveFile(string sourceFilePath, string organizedSubPath, string extension)
{
    FileInfo fileInfo = new FileInfo(sourceFilePath);
    DateTime dateTime = fileInfo.LastWriteTime;

    // Separated the date for readability.
    string year = dateTime.Year.ToString();
    string month = dateTime.ToString("MMMM");

    string fileName = Path.GetFileName(sourceFilePath);
    // Create a path name that separates the year, month, and day.
    string subDatePath = $@"{organizedSubPath}\{year}\{month}\";
    if (organizedSubPath == "Others" || organizedSubPath == "Installer file")
    {
        subDatePath = $@"{organizedSubPath}\";
    }

    // Combine the organized directory path and sub date paths.
    string fullPath = Path.Combine(destinationPath, subDatePath);

    Directory.CreateDirectory(fullPath);

    // File's organized directory path
    string organizedFilePath = Path.Combine(fullPath, fileName);

    // Check if it exists
    if (File.Exists(organizedFilePath))
    {
        fileName = $"{Path.GetFileNameWithoutExtension(organizedFilePath)}-{DateTime.Now.Minute}{DateTime.Now.Second}.{extension}";
        organizedFilePath = Path.Combine(fullPath, fileName);
    }

    // Keeps on looping if the file is not ready yet.
    while (IsFileLocked(sourceFilePath) || IsFileLocked(organizedFilePath, true))
        Thread.Sleep(5000);

    // Actual moving of file.
    File.Move(sourceFilePath, organizedFilePath);

    // Logs file that was transferred/moved.
    File.AppendAllText("Log.csv", $"{DateTime.Now},{sourceFilePath},{organizedFilePath},{Environment.NewLine}");
}

// Checks if file is still transferring or in used.
bool IsFileLocked(string filePath, bool delete = false)
{
    bool locked = false;
    try
    {
        FileStream fileStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        fileStream.Close();
        if (delete)
            File.Delete(filePath);
    }
    catch (Exception)
    {
        locked = true;
    }
    return locked;
}