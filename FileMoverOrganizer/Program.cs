string sourcePath, destinationPath;

Dictionary<string, string> organizedSubPaths = new Dictionary<string, string>();

string[] config = File.ReadAllLines("config.csv");

sourcePath = config[0].Split(',')[1];
destinationPath = config[1].Split(',')[1];

LoadExtensionSubPaths();

if(Directory.Exists(sourcePath))
{
    ProcessDirectory(sourcePath);
}

void ProcessDirectory(string targetDirectory)
{
    string[] fileEntries = Directory.GetFiles(targetDirectory);
    foreach (string fileName in fileEntries)
        ProcessFiles(fileName);

    string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
    foreach (string subdirectory in subdirectoryEntries)
    {
        MoveDirectory(subdirectory);
        Console.WriteLine($@"Directory has been moved to {destinationPath}\Directories");
    }
}

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

void LoadExtensionSubPaths()
{
    for (int i = 2; i < config.Length; i++)
    {
        string[] extensionAndSubPath = config[i].Split(",");
        organizedSubPaths.Add(extensionAndSubPath[0], extensionAndSubPath[1]);
    }
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

    // file's organized directory path
    string organizedFilePath = Path.Combine(fullPath, fileName);

    // Check if it exists
    if (File.Exists(organizedFilePath))
    {
        fileName = $"{Path.GetFileNameWithoutExtension(organizedFilePath)}-{DateTime.Now.Minute}{DateTime.Now.Second}.{extension}";
        organizedFilePath = Path.Combine(fullPath, fileName);
    }

    // Keeps on looping if the file is not ready yet.
    while (IsFileLocked(sourceFilePath) || IsFileLocked(organizedFilePath, true)) ;

    // Actual moving of file.
    File.Move(sourceFilePath, organizedFilePath);

    // Logs file that was transferred/moved.
    File.AppendAllText("Log.csv", $"{DateTime.Now},{sourceFilePath},{organizedFilePath},{Environment.NewLine}");
}

void MoveDirectory(string sourceDirectoryPath)
{
    string folderName = Path.GetFileName(sourceDirectoryPath);
    // Create a path name that stores all directories.
    string subPath = @"Directories\";

    // Combine the destination directory path and sub path.
    string destinationDirectoryPath = Path.Combine(destinationPath, subPath);

    Directory.CreateDirectory(destinationDirectoryPath);

    destinationDirectoryPath = Path.Combine(destinationDirectoryPath, folderName);

    if (Directory.Exists(destinationDirectoryPath))
    {
        destinationDirectoryPath = $"{destinationDirectoryPath}-{DateTime.Now.Date.ToString("ddd")}-{DateTime.Now.Minute}-{DateTime.Now.Second}";
    }
    Directory.Move(sourceDirectoryPath, destinationDirectoryPath);
    File.AppendAllText("Log.csv", $"{DateTime.Now},{sourceDirectoryPath},{destinationDirectoryPath},{Environment.NewLine}");
}

// Checks if file is still transferring or in used.
bool IsFileLocked(string watchFilePath, bool del = false)
{
    bool locked = false;
    try
    {
        FileStream fs = File.Open(watchFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        fs.Close();
        if (del)
            File.Delete(watchFilePath);
    }
    catch (Exception)
    {
        locked = true;
    }
    return locked;
}