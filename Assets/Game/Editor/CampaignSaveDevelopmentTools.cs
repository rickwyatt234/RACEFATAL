using System.IO;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.FrontEnd;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampaignSaveDevelopmentTools
{
    public static void OpenSaveFolder()
    {
        string root = BootstrapController.CampaignSaveDirectory;
        Directory.CreateDirectory(root);
        EditorUtility.RevealInFinder(root);
    }

    public static void DeleteAllSaveSlots()
    {
        if (!EditorUtility.DisplayDialog("Delete all three campaign saves?",
                "All local campaign slots and their backups will be erased. A loaded campaign will close without saving.",
                "Delete all three", "Cancel")) return;

        var context = Application.isPlaying ? BootstrapController.Context : null;
        var repository = new JsonCampaignSaveRepository(BootstrapController.CampaignSaveDirectory);
        var saves = context?.Saves;
        int? active = saves?.ActiveSlotIndex;
        // Remove the active slot last so an earlier failure leaves the open session usable.
        for (int i = 1; i <= repository.SlotCount; i++)
        {
            if (i == active) continue;
            var result = repository.Delete(i);
            if (!result.IsSuccess) { ReportFailure(result.ErrorMessage); return; }
        }
        if (active.HasValue)
        {
            var result = repository.Delete(active.Value);
            if (!result.IsSuccess) { ReportFailure(result.ErrorMessage); return; }
            saves.CloseCurrentCampaign(false);
            context.RaceLaunch.Clear();
            SceneManager.LoadScene("03_MainMenu");
        }
        else RefreshSlots();
        Debug.Log("[RF Developer Tools] All three campaign slots are empty.");
    }

    private static void ReportFailure(string error)
    {
        RefreshSlots();
        EditorUtility.DisplayDialog("Could not clear every slot", error, "OK");
    }

    private static void RefreshSlots()
    {
        if (!Application.isPlaying) return;
        Object.FindFirstObjectByType<CampaignSelectController>()?.Refresh();
    }
}
