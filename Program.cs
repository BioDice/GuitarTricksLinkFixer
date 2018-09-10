using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace console
{
  class Program
  {
    public static string curPath;
    public static string _rgxActiveList = @"<li .*class=\""(?:.*)\bcurrent_lesson\b(?:.*)\""><a.href=\""{0}\"">.*?<\/li>";
    public static string _rgxIdFromLink = @"\/([a-z]*).(.*)?\?input=([0-z]*).*?";
    public static string _rgbEveryLink = @"<a href=\""\/(([a-z]*).(.*)?\?input=([0-z]*).*?)\"">";


    public static List<string> allFiles = new List<string>();

    static void Main(string[] args)
    {
      // Adding JSON file into IConfiguration.
      IConfiguration config = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", true, true)
      .Build();

      string srcPath = config["path"];
      string destPath = srcPath + " (copied)";
      curPath = destPath;

      CopyDir(srcPath, destPath);
      GetListOfFiles(destPath);

      StartFixingLinkProcess();
    }

    public static void CopyDir(string srcPath, string destPath)
    {
      if (Directory.Exists(destPath))
      {
        Console.WriteLine("Directory '{0}' already exists.. Deleting it now.", destPath);
        Directory.Delete(destPath, true);
      }

      //Now Create all of the directories
      foreach (string dirPath in Directory.GetDirectories(srcPath, "*", SearchOption.AllDirectories))
      {
        string newDir = dirPath.Replace(srcPath, destPath);
        Console.WriteLine("Creating directory {0}", newDir);
        Directory.CreateDirectory(newDir);
      }

      //Copy all the files & Replaces any files with the same name
      foreach (string newPath in Directory.GetFiles(srcPath, "*.*", SearchOption.AllDirectories))
      {
        File.Copy(newPath, newPath.Replace(srcPath, destPath), true);
      }
    }

    public static void GetListOfFiles(string srcPath)
    {
      var ext = new List<string> { ".php" };
      allFiles = Directory.GetFiles(srcPath, "*.*", SearchOption.AllDirectories)
          .Where(s => ext.Contains(Path.GetExtension(s))).ToList();

      Console.WriteLine("Reading a total of {0} files", allFiles.Count);
    }

    public static void StartFixingLinkProcess()
    {
      foreach (string file in allFiles)
      {
        Console.WriteLine("Processing '{0}'", file);
        string content = File.ReadAllText(file);
        string filename = Path.GetFileName(file);
        string id = GetIdFromContent(content, filename);
        string dir = Path.GetDirectoryName(file);

        if (!string.IsNullOrEmpty(id))
        {
          string newContent = FixEveryLinkInContent(content, filename);
          File.WriteAllText(file, newContent);
          string newFileName = Path.Combine(dir, id);
          RenameFile(file, newFileName);
        }
        else
        {
          Console.WriteLine("WARNING: Couldn't find ID of file '{0}'", file);
          Console.WriteLine("WARNING: Deleting file '{0}' !!!", file);
          File.Delete(file);
          continue;
        }
      }
    }

    public static string GetIdFromContent(string content, string filename)
    {
      string retVal = "";
      try
      {
        string regex = string.Format(_rgxActiveList, _rgxIdFromLink);

        MatchCollection collection = Regex.Matches(content, regex);
        int matchCount = collection.Count;
        if (matchCount > 1)
        {
          Console.WriteLine("WARNING: {0} | There are {1} matched ID's found", filename, matchCount);
        }
        else if (matchCount == 1)
        {
          Console.WriteLine("INFO: {0} | ID FOUND", filename, matchCount);
        }

        foreach (Match match in collection)
        {
          List<string> id = new List<string>();
          foreach (Group group in match.Groups)
          {
            id.Add(group.Value);
          }

          retVal = id[1] + "-" + id[3] + "." + id[2];
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
      }

      return retVal;
    }

    public static string FixEveryLinkInContent(string content, string filename)
    {
      try
      {
        StringDictionary dictionary = new StringDictionary();
        MatchCollection collection = Regex.Matches(content, _rgbEveryLink);
        Console.WriteLine("INFO: {0} | A total of {1} links are found for fixing", filename, collection.Count);
        foreach (Match match in collection)
        {
          List<string> segments = new List<string>();
          foreach (Group group in match.Groups)
          {
            segments.Add(group.Value);
          }

          string id = segments[2] + "-" + segments[4] + "." + segments[3];
          if (!dictionary.ContainsKey(segments[1]))
          {
            dictionary.Add(segments[1], id);
          }
        }

        StringBuilder result = new StringBuilder(content);
        foreach (DictionaryEntry entry in dictionary)
        {
          result.Replace(entry.Key.ToString(), entry.Value.ToString());
        }
        Console.WriteLine("INFO: {0} | A total of {1} links are fixed", filename, dictionary.Count);
        return result.ToString();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
      }

      return null;
    }

    public static void RenameFile(string oldFilename, string newFilename)
    {
      try
      {
        Console.WriteLine("INFO: Renaming '{0}' to '{1}' ", oldFilename, newFilename);
        File.Move(oldFilename, newFilename);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
      }
    }
  }
}
