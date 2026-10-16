using System.IO;
using UnityEngine;

namespace BattleRoyale.Company
{
    public sealed class CompanySaveFile : MonoBehaviour
    {
        [SerializeField] string fileName = "company-save.json";

        public bool Exists()
        {
            return File.Exists(SavePath());
        }

        public bool TryLoad(out CompanySaveRecord record)
        {
            record = null;
            var path = SavePath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (IOException)
            {
                Debug.LogError("Company save could not be read.", this);
                return false;
            }

            if (CompanySaveRules.TryRead(json, out record))
            {
                return true;
            }

            Debug.LogError("Company save could not be parsed.", this);
            return false;
        }

        public bool TrySave(CompanySaveRecord record)
        {
            if (!CompanySaveRules.TryWrite(record, out var json))
            {
                Debug.LogError("Company save could not be written.", this);
                return false;
            }

            var path = SavePath();
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Company save has no file name.", this);
                return false;
            }

            try
            {
                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllText(path, json);
            }
            catch (IOException)
            {
                Debug.LogError("Company save could not be written.", this);
                return false;
            }

            return true;
        }

        string SavePath()
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}
