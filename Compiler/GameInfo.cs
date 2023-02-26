﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace Rbx2Source.Compiler
{
    public class GameInfo
    {
        public string GameName;
        public string GameInfoPath;

        public string GameDirectory;
        public string RootDirectory;

        public string StudioMdlPath;

        public string HLMVPath;
        public Icon GameIcon;

        public bool ReadyToUse => (GameInfoPath != null && StudioMdlPath != null);

        private static readonly IReadOnlyDictionary<string, string> PREFERRED_GAMEINFO_DIRECTORIES = new Dictionary<string, string>()
        {
            {"Half-Life 2", "hl2"}
        };

        private static void usePathIfExists(string path, ref string setTo)
        {
            if (File.Exists(path))
            {
                setTo = path;
            }
        }

        public GameInfo(string path)
        {
            string fileName = Path.GetFileName(path);

            if (fileName != "gameinfo.txt")
                throw new Exception("Expected gameinfo.txt file.");

            string gameInfo = File.ReadAllText(path);

            StringReader reader = new StringReader(gameInfo);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Replace("\t", "");
                line = line.TrimStart(' ');
                line = line.Replace("\"game\"", "game");

                if (line.StartsWith("game", StringComparison.InvariantCulture))
                {
                    int firstQuote = line.IndexOf('"');

                    if (firstQuote > 0)
                    {
                        int lastQuote = line.IndexOf('"', ++firstQuote);

                        if (lastQuote > 0)
                        {
                            GameName = line.Substring(firstQuote, lastQuote - firstQuote);
                            break;
                        }
                    }
                }
            }

            if (GameName == null)
                throw new Exception("Invalid gameinfo.txt file: Couldn't identify the name of the game.");

            // Fix cases where Valve likes to SCREAM the name of their game.
            MatchCollection matches = Regex.Matches(GameName, "[A-Z]+");

            foreach (Match match in matches)
            {
                foreach (Group group in match.Groups)
                {
                    string allCapStr = group.ToString();
                    if (allCapStr.Length > 2)
                    {
                        string newStr = allCapStr.Substring(0,1) + allCapStr
                            .Substring(1)
                            .ToLowerInvariant();

                        GameName = GameName.Replace(allCapStr, newStr);
                    }
                }
            }

            GameName = GameName.Replace(" [Beta]", "");
            GameName = GameName.Replace(" Source", ": Source");
            GameName = GameName.Replace(" DM", ": Deathmatch");

            GameInfoPath = path;
            GameDirectory = Directory.GetParent(path).ToString();
            RootDirectory = Directory.GetParent(GameDirectory).ToString();

            if (PREFERRED_GAMEINFO_DIRECTORIES.ContainsKey(GameName))
            {
                string preferred = PREFERRED_GAMEINFO_DIRECTORIES[GameName];
                GameDirectory = Path.Combine(RootDirectory, preferred);
                GameInfoPath = Path.Combine(GameDirectory, "gameinfo.txt");
            }
            
            string binPath = Path.Combine(RootDirectory, "bin");

            if (Directory.Exists(binPath))
            {
                string studioMdlPath = Path.Combine(binPath, "studiomdl.exe");
                usePathIfExists(studioMdlPath, ref StudioMdlPath);
                
                string hlmvPath = Path.Combine(binPath, "hlmv.exe");
                usePathIfExists(hlmvPath, ref HLMVPath);
            }

            string resources = Path.Combine(GameDirectory, "resource");

            if (Directory.Exists(resources))
            {
                string gameIco = Path.Combine(resources, "game.ico");
                
                if (File.Exists(gameIco))
                {
                    Icon iconFile = Icon.ExtractAssociatedIcon(gameIco);
                    GameIcon = iconFile;
                }
            }

            if (GameIcon == null)
            {
                foreach (string file in Directory.GetFiles(RootDirectory))
                {
                    if (file.EndsWith(".exe", StringComparison.InvariantCulture) || file.EndsWith(".ico", StringComparison.InvariantCulture))
                    {
                        GameIcon = Icon.ExtractAssociatedIcon(file);
                        break;
                    }
                }
            }

            reader.Dispose();
        }
    }
}
