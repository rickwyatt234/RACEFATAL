using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class MonoBehaviour { public GameObject gameObject = new GameObject(); protected static T FindFirstObjectByType<T>() where T:class => null; public T GetComponentInChildren<T>(bool includeInactive) where T:class=>null; }
    public class GameObject { public bool activeSelf=true; public void SetActive(bool value)=>activeSelf=value; }
    public class CanvasGroup { public float alpha; public bool interactable,blocksRaycasts; }
    public sealed class SerializeField:Attribute {}
    public sealed class Header:Attribute {public Header(string value){} }
    public sealed class Tooltip:Attribute {public Tooltip(string value){} }
    public sealed class Range:Attribute {public Range(float min,float max){} }
    public static class Cursor {public static CursorLockMode lockState;public static bool visible;}
    public enum CursorLockMode {None}
    public static class Debug {public static void LogError(string message,object context=null){} }
    public static class Application {public static bool CanStreamedLevelBeLoaded(string name)=>true;}
}
namespace UnityEngine.UI
{
    public class Button:UnityEngine.MonoBehaviour {public bool interactable; public ClickEvent onClick=new ClickEvent();public void Select(){} }
    public class ClickEvent { public void AddListener(Action action){} public void RemoveListener(Action action){} }
}
namespace TMPro {public class TextMeshProUGUI {public string text;public bool enableAutoSizing;public float fontSizeMin,fontSizeMax;} }
namespace UnityEngine.SceneManagement {public static class SceneManager {public static string Loaded;public static void LoadScene(string name)=>Loaded=name;} }
namespace RaceFatal.Shared {public class Result {public bool IsSuccess=true;public string ErrorMessage;} }
namespace RaceFatal.Career
{
    public class RaceReward {public int Credits,TeamFame,ResearchPoints,CharacterFame;}
    public class PostRaceResult {public RaceReward Reward=new RaceReward();public float AverageAudienceFavor,AudienceFameMultiplier=1;public int RaceResearchPoints,EventResearchPoints,ResearcherPoints;public bool PlayerDied,CareerEnded;}
}
namespace RaceFatal.Racing
{
    public enum RaceParticipantRole {Player}
    public class RaceParticipant {public string RacerId;public RaceParticipantRole Role;public int FinishPosition;public string EliminationReason;}
    public class RaceResult {public object Deathmatch;}
    public class RaceState {public object Deathmatch;public List<RaceParticipant> Participants=new List<RaceParticipant>();}
    public class RaceDirector
    {
        public RaceState State=new RaceState();public RaceFatal.Career.PostRaceResult PostRaceResult;
        public event Action<RaceParticipant> RacerFinished,RacerRetired,RacerDestroyed;
        public event Action<RaceFatal.Career.PostRaceResult> PostRaceResolved;
        public event Action<RaceResult> RaceCompleted;
        public RaceResult ResolveRemainingRace()=>new RaceResult();
    }
}
namespace RaceFatal.Presentation.Vehicles
{
    public class PlayerCockpitHUD {public void SetHudVisible(bool value){} }
    public class PlayerCockpitView {public void SetReticleVisible(bool value){} }
}
namespace RaceFatal.Presentation.Racing
{
    public class RacerViewController:UnityEngine.MonoBehaviour {}
    public class RaceRuntimeController {public bool IsInitialized;public RaceFatal.Racing.RaceDirector Director;public bool TryGetRacerView(string id,out RacerViewController view){view=null;return false;}public void StopRacerControl(string id,float brake){}public void StopAllVehicleControl(float brake){} }
    public class RaceStandingsView:UnityEngine.MonoBehaviour {public void Initialize(RaceRuntimeController runtime){} public void ShowLive(){} public void ShowFinal(RaceFatal.Racing.RaceResult result){} }
}
namespace RaceFatal.Infrastructure
{
    public class GameContext {public SaveService Saves=new SaveService();public LaunchContext RaceLaunch=new LaunchContext();}
    public class SaveService {public bool HasActiveCampaign=true;public RaceFatal.Shared.Result SaveCurrentCampaign()=>new RaceFatal.Shared.Result();}
    public class LaunchContext {public void Clear(){} }
}
namespace RaceFatal.Presentation.Bootstrap {public static class BootstrapController {public static RaceFatal.Infrastructure.GameContext Context=new RaceFatal.Infrastructure.GameContext();} }
