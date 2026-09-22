using System.IO;
using UnityEditor;
using UnityEngine;

public static class CampaignSaveDevelopmentTools
{
    [MenuItem(
        "RACE//FATAL/Development/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        string saveRoot =
            GetSaveRoot();

        Directory.CreateDirectory(
            saveRoot);

        EditorUtility.RevealInFinder(
            saveRoot);
    }

    [MenuItem(
        "RACE//FATAL/Development/Delete All Save Slots")]
    public static void DeleteAllSaveSlots()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "RACE//FATAL",
                "Stop Play Mode before deleting campaign save slots.",
                "OK");

            return;
        }

        bool confirmed =
            EditorUtility.DisplayDialog(
                "Delete All Campaign Saves?",
                "This permanently deletes every campaign save slot, including backup and temporary slot files, from the local RACE//FATAL save directory.",
                "Delete All Saves",
                "Cancel");

        if (!confirmed)
            return;

        string saveRoot =
            GetSaveRoot();

        if (!Directory.Exists(
                saveRoot))
        {
            EditorUtility.DisplayDialog(
                "RACE//FATAL",
                "No campaign save directory exists yet.",
                "OK");

            return;
        }

        string[] files =
            Directory.GetFiles(
                saveRoot,
                "slot_*",
                SearchOption.TopDirectoryOnly);

        int deletedCount = 0;

        foreach (string file in files)
        {
            File.Delete(
                file);

            deletedCount++;
        }

        if (Directory.GetFiles(
                saveRoot,
                "*",
                SearchOption.TopDirectoryOnly).Length == 0 &&
            Directory.GetDirectories(
                saveRoot,
                "*",
                SearchOption.TopDirectoryOnly).Length == 0)
        {
            Directory.Delete(
                saveRoot);
        }

        Debug.Log(
            $"RACE//FATAL deleted {deletedCount} campaign save file(s).");

        EditorUtility.DisplayDialog(
            "RACE//FATAL",
            deletedCount > 0
                ? $"Deleted {deletedCount} campaign save file(s)."
                : "No campaign save slot files were found.",
            "OK");
    }

    private static string GetSaveRoot()
    {
        return Path.Combine(
            Application.persistentDataPath,
            "RACEFATAL",
            "Saves");
    }
}
