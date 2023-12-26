using System;
using System.Collections.Generic;
using MetadataExtractor;
using Directory = MetadataExtractor.Directory;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        Console.WriteLine("Starting movement and renaming of files");

        string rootDirectory;

        do
        {
            Console.Write("Enter the root directory path: ");
            rootDirectory = Console.ReadLine();
        } while (!IsDirectoryValid(rootDirectory));

        OrganizeJpgFiles(rootDirectory);

        Console.WriteLine("All files have been organized into folders by creation date in the root directory.");
    }

    static bool IsDirectoryValid(string path)
    {
        try
        {
            // Check if the path is rooted and if it points to an existing directory
            if (Path.IsPathRooted(path) && new DirectoryInfo(path).Exists)
            {
                return true;
            }
            else
            {
                Console.WriteLine("Invalid directory path. Please enter a valid absolute path.");
                return false;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error checking directory validity: {exception.Message}");
            return false;
        }
    }


    static void OrganizeJpgFiles(string rootDirectory)
    {
        // Get all photo or video file
        var allowedExtensions = new[] { "*.jpg", "*.HEIC", "*.png", "*.JPEG", "*.MOV", "*.MP4" };

        var files = new DirectoryInfo(rootDirectory)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(file => allowedExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            Console.WriteLine($"Processing file: {file.FullName}");

            try
            {
                // Read metadata for the current JPG file
                IEnumerable<Directory> directories = ImageMetadataReader.ReadMetadata(file.FullName);

                // Extract the Date/Time Original property
                var dateTimeOriginal = ExtractDateTimeOriginal(directories);

                if (dateTimeOriginal != null)
                {
                    // Format the creation date to "yyyy-MM-dd" format
                    var folderName = dateTimeOriginal.Value.ToString("yyyy-MM-dd");

                    // Create new folder if needed
                    CreateFolder(rootDirectory, folderName);

                    // Rename file and move to folder
                    RenameAndMoveFile(file.FullName, file.Extension, dateTimeOriginal.Value, rootDirectory+"\\"+folderName);
                }
                else
                {
                    Console.WriteLine("Date/Time Original property not found.");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error processing file {file.FullName}: {exception.Message}");
            }
        }
    }

    private static void RenameAndMoveFile(string originalFileName, string originalExtension, DateTimeOffset value, string destinationDirectory)
    {
        // Format the date to "yyyy-MM-dd HH:mm" format
        var formattedDateTime = value.ToString("yyyy-MM-dd HHmm");

        // Construct the new file name
        var newFileName = $"{destinationDirectory}\\{formattedDateTime}{originalExtension}";

        // Check if the file already exists
        if (File.Exists(newFileName))
        {
            int counter = 2;

            // Keep incrementing the counter until finding a non-existing filename
            while (File.Exists(newFileName))
            {
                // Modify the new file name by appending the counter
                newFileName = $"{destinationDirectory}\\{formattedDateTime} ({counter}){originalExtension}";
                counter++;
            }
        }

        // Move the file to the new file name
        File.Move(originalFileName, newFileName);
        Console.WriteLine($"File renamed to: {newFileName}");
    }

    private static void CreateFolder(string rootDirectory, string folderName)
    {
        var folderPath = Path.Combine(rootDirectory, folderName);
        var directoryInfo = new DirectoryInfo(folderPath);

        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
        }
    }

    static DateTimeOffset? ExtractDateTimeOriginal(IEnumerable<Directory> directories)
    {
        // Extract the Date/Time Original property
        var dateTimeOriginalTag = directories.SelectMany(directory => directory.Tags)
            .FirstOrDefault(tag => tag.Name == "Date/Time Original");

        if (dateTimeOriginalTag is null)
        {
            // for movie files
            dateTimeOriginalTag = directories.SelectMany(directory => directory.Tags)
            .FirstOrDefault(tag => tag.Name == "Created");

            // Define the format of the input string
            string format = "ddd MMM dd HH:mm:ss yyyy";

            // Parse the string into a DateTimeOffset object
            DateTimeOffset dateTimeOffset = DateTimeOffset.ParseExact(dateTimeOriginalTag.Description!, format, null);

            return dateTimeOffset;
        }

        if (dateTimeOriginalTag is null)
        {
            dateTimeOriginalTag = directories.SelectMany(directory => directory.Tags)
            .FirstOrDefault(tag => tag.Name == "File Modified Date");

            // Define the format of the input string
            string format = "ddd MMM dd HH:mm:ss zzzz yyyy";

            // Parse the string into a DateTimeOffset object
            DateTimeOffset dateTimeOffset = DateTimeOffset.ParseExact(dateTimeOriginalTag.Description!, format, null);

            return dateTimeOffset;
        }

        string otherFormat = "yyyy:MM:dd HH:mm:ss";

        return DateTimeOffset.ParseExact(dateTimeOriginalTag.Description!, otherFormat, null);
    }
}
