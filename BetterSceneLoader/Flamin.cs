using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UILib;
using IllusionPlugin;
using System.IO;
using System.Collections;
using System.Diagnostics;
using Studio;
using System.Runtime.InteropServices;

namespace BetterSceneLoader
{
    public static class Flamin {

        public static string backupFolderPath = "";
        static string _backupFolderName = "zz Backups Before Save";
        static int _countBackups = 500;
        static string _lastResavedFilePath = "";
        static DateTime _lastResavedTime = DateTime.Now;


        public static void Init() {
            LoadSettings();
            CreateBackupsFolders();
        }

        public static void BackupResavedSceneFile(string file_path, bool is_removed = false) {
            if (_countBackups < 1 || file_path == "" || !File.Exists(file_path) || file_path.Contains(_backupFolderName))
                return;

            // Копируем файл ПЕРЕД Перезаписью/Удалением.

            // 1) Бекапим сначала файл, если он удаляется, либо если тот же самый файл пересохраняется
            //    после предыдущего пересохранения не сразу, а хотя бы через 4 сек.
            if (_lastResavedFilePath != file_path || (DateTime.Now - _lastResavedTime).TotalSeconds > 4) {
                string orig_folder_name = Path.GetFileName(Path.GetDirectoryName(file_path));

                string filename =
                    "(" + DateTime.Now.ToString("yyyy-MM-dd в HH-mm-ss.fff")
                    + ") (" + orig_folder_name + ") "
                    + Path.GetFileName(file_path);

                File.Copy(file_path, backupFolderPath + "/" + filename);

                // При пересохранении запоминаем путь до файла и время
                if (!is_removed) {
                    _lastResavedFilePath = file_path;
                    _lastResavedTime = DateTime.Now;
                }
                else
                    _lastResavedFilePath = "";
            }
            else
                _lastResavedTime = DateTime.Now;

            // Удаляем слишком старые файлы.
            string[] files = Directory.GetFiles(backupFolderPath, "*.png", SearchOption.TopDirectoryOnly);

            if (files.Length > _countBackups) {
                int files_to_delete = files.Length - _countBackups;
                for (int file_i = 0; file_i < files_to_delete; file_i++)
                    File.Delete(files[0]);
            }
        }



        static void LoadSettings() {
            _countBackups = ModPrefs.GetInt("BetterSceneLoader", "CountBackups", 500, true);
        }

        public static void CreateBackupsFolders() {
            // (для создания папки исп-ем вариант не под ModOrginizer, иначе папка будет в конце списка)
            backupFolderPath = BetterSceneLoader.scenePath + "/" + _backupFolderName;

            if (!Directory.Exists(backupFolderPath))
                Directory.CreateDirectory(backupFolderPath);

            backupFolderPath = ModOrginizerPathFix(backupFolderPath);
        }

        public static string ModOrginizerPathFix(string path) {
            return path.Replace("data\\UserData\\", "MOHS\\overwrite\\UserData\\").Replace("Data\\UserData\\", "MOHS\\overwrite\\UserData\\")
                       .Replace("data/UserData/", "MOHS/overwrite/UserData/").Replace("Data/UserData/", "MOHS/overwrite/UserData/");
        }
        
    }
}
