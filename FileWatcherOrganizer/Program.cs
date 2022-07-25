string watchPath, organizedPath;
// string (1): extension name.
// string (2): subfolder or sub path.
Dictionary<string, string> organizedSubPaths = new Dictionary<string, string>();

string[] config = File.ReadAllLines("config.csv");

// Read the first two lines to read the paths needed.
// Split the string with a comma (csv).
watchPath = config[0].Split(',')[1];
organizedPath = config[1].Split(',')[1];

// Read the rest of the config array to get the extension and sub path folder.
for (int i = 2; i < config.Length; i++)
{
    string[] extensionAndSubPath = config[i].Split(",");
    organizedSubPaths.Add(extensionAndSubPath[0], extensionAndSubPath[1]);
}

using (FileSystemWatcher watcher = new FileSystemWatcher(watchPath))
{
    watcher.Path = watchPath;
    watcher.Filter = "*.*";
    watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;

    watcher.Created += OnCreated;
    watcher.EnableRaisingEvents = true;

    Console.WriteLine("Press x to exit");
    while (Console.Read() != 'x') ;
}

void OnCreated(object sender, FileSystemEventArgs e)
{

    string watchFilePath = e.FullPath;
    // Notifies if it founds a new file.
    Console.WriteLine($"Found a new file: {Path.GetFileName(e.FullPath)}");
    // Ignore '.'
    string extension = Path.GetExtension(watchFilePath);
    extension = String.IsNullOrEmpty(extension) ? extension : extension.Substring(1);

    if (organizedSubPaths.ContainsKey(extension.ToLower()))
    {
        MoveFile(watchFilePath, organizedSubPaths[extension.ToLower()], extension);
    }
    // Notifies if the file move was successful.
    Console.WriteLine($"File has been moved to {organizedPath}");
}

void MoveFile(string watchFilePath, string organizedSubPath, string extension)
{
    DateTime today = DateTime.Now;
    
    // Separated the date for readability.
    string year = today.Year.ToString();
    string month = today.ToString("MMMM");
    string day = today.ToString("dd");

    string fileName = Path.GetFileName(watchFilePath);
    // Create a path name that separates the year, month, and day.
    string subDatePath = $@"{organizedSubPath}\{year}\{month}\{day}\";

    // Combine the organized directory path and sub date paths.
    string fullPath = Path.Combine(organizedPath, subDatePath);

    Directory.CreateDirectory(fullPath);

    // file's organized directory path
    string organizedFilePath = Path.Combine(fullPath, fileName);

    // Check if it exists
    if (File.Exists(organizedFilePath))
    {
        fileName = $"{Path.GetFileNameWithoutExtension(organizedFilePath)}-{today.Minute}{today.Second}.{extension}";
        organizedFilePath = Path.Combine(fullPath, fileName);
    }
    // Checks if file is still transferring or in used.
    while (IsFileLocked(watchFilePath) || IsFileLocked(organizedFilePath, true)) ;
    File.Move(watchFilePath, organizedFilePath);
    // Logs file that was transferred/moved.
    File.AppendAllText("Log.csv", $"{today},{watchFilePath},{organizedFilePath},{Environment.NewLine}");
}

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