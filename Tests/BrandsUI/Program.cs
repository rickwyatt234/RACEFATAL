using System;
using System.Linq;
using System.Reflection;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure;
using RaceFatal.Presentation.Career;
using RaceFatal.Shared;
using RaceFatal.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

static class Program
{
    static int checks;
    static void Check(bool value, string message) {checks++;if(!value)throw new Exception(message);}
    static object Field(object instance,string name)=>instance.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(instance);
    static void Set(object instance,string name,object value)=>instance.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(instance,value);
    static void Call(object instance,string name,params object[] args)=>instance.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(instance,args);
    static void Main()
    {
        var db=new GameDatabase();
        db.AddBikeDefinition(new BikeDefinition("bike","Bike",1,0,0));
        db.AddEngineDefinition(new EngineDefinition("engine","Engine",default,100,10,100));
        db.AddChassisDefinition(new ChassisDefinition("chassis","Chassis",250,1,100));
        db.AddBikeBuildDefinition(new BikeBuildDefinition("build","Build","bike","engine","chassis",Array.Empty<EquipmentMountDefinition>()));
        db.AddRacerDefinition(new RacerDefinition("partner","Partner",.5f,.5f,.5f,.5f,.5f));
        BrandDefaults.Ensure(db);
        var career=new CareerManager(new CharacterFactory());var builds=new BikeBuildFactory(db,new VehicleFactory(),new EquipmentFactory());
        var sessions=new GameSessionManager(db,career,new WorldFactory(db,builds),builds);
        var session=sessions.CreateNewSession(new NewGameRequest("Team","#fff","#000","Driver","partner","build","build")).Value;
        session.PlayerTeam.AddFame(10000);
        var context=new GameContext {Database=db,Sessions=sessions,Saves=new SaveService()};
        var missing=new GameObject("Missing",typeof(RectTransform));
        int before=GameObject.Created;
        Check(CareerBrandsView.InitializeExisting(missing,null,context)==null && GameObject.Created==before,"Missing layout logs an error without creating UI");
        var root=new GameObject("BrandsRoot",typeof(RectTransform));var view=root.AddComponent<CareerBrandsView>();
        BrandUiLayout.Build(root.transform,view);
        Check(view.HasAuthoredLayout,"Editor builder authors a complete layout");
        var slots=(CareerBrandsView.UiSlot[])Field(view,"slots");
        Check(slots.GroupBy(s=>new {s.parent,s.key}).All(g=>g.Count()==1),"Authored slot identities are unique");
        int objects=GameObject.Created,components=GameObject.ComponentsAdded;
        Check(CareerBrandsView.InitializeExisting(root,null,context)==view,"Runtime binds existing view");
        void Refresh(int tab,int filter=0){Set(view,"tab",tab);Set(view,"contractFilter",filter);view.Refresh();}
        var pages=(RectTransform[])Field(view,"pages");
        var balances=(TMP_Text)Field(view,"balances");balances.fontSize=39;balances.rectTransform.anchoredPosition=new Vector2(123,-99);
        foreach(int tab in Enumerable.Range(0,5)){Refresh(tab);Check(pages[tab].gameObject.activeSelf,"Requested page remains authored and visible");}
        foreach(var brand in db.BrandDefinitions.Values.Take(2))view.FocusBrand(brand.Id);
        ((TMP_InputField)Field(view,"search")).onEndEdit.Invoke("NO SUCH BRAND");
        ((TMP_InputField)Field(view,"search")).onEndEdit.Invoke("");
        var service=new BrandService(db,new Random(1));
        var receipt=service.OpenBox(session.PlayerTeam).Value;
        view.FocusBrand(receipt.brandId); // Discovered catalog and its prebuilt ScrollRect.
        session.PlayerTeam.Brands.pendingReveal=null;
        foreach(var contract in session.PlayerTeam.Brands.contracts)service.SetActive(session.PlayerTeam,contract.instanceId,true);
        Refresh(0);Refresh(2,1);
        foreach(var contract in session.PlayerTeam.Brands.contracts){contract.active=false;contract.progress=contract.terms.Target;contract.completed=true;}
        Refresh(2,2);foreach(var contract in session.PlayerTeam.Brands.contracts)contract.claimed=true;
        Refresh(2,3);Refresh(2,0);
        Call(view,"Confirm","Confirm details",(Action)(()=>{}));
        Check(((GameObject)Field(view,"modal")).activeSelf,"Confirmation reuses saved overlay");Call(view,"CloseModal");
        session.PlayerTeam.Brands.pendingReveal=receipt;
        Call(view,"StartReveal");
        var coroutine=MonoBehaviour.Pending;
        Check(coroutine!=null,"Reveal starts using authored controls");Set(view,"skip",true);
        while(coroutine.MoveNext()){}
        var continueSlot=slots.Single(s=>s.rect.gameObject.name=="SkipOrContinue");continueSlot.button.onClick.Invoke();
        Check(session.PlayerTeam.Brands.pendingReveal==null,"Reveal acknowledgement persists and clears saved receipt");
        Call(view,"OnDisable");
        Check(GameObject.Created==objects && GameObject.ComponentsAdded==components,"All pages, empty/full lists, confirmations, and reveal add no objects or components at runtime");
        Check(balances.fontSize==39 && balances.rectTransform.anchoredPosition.x==123,"Runtime refresh preserves designer fonts and positions");
        view.PreviewEditorPage(5);Check(!continueSlot.rect.gameObject.activeSelf,"Confirmation preview hides reveal controls");
        view.PreviewEditorPage(6);Check(continueSlot.rect.gameObject.activeSelf,"Reveal preview shows reveal controls");
        var canvas = new GameObject("CareerCanvas", typeof(RectTransform));
        var menuRoot = new GameObject("BrandsRoot", typeof(RectTransform)); menuRoot.transform.SetParent(canvas.transform, false);
        new GameObject("CareerSystems").AddComponent<CareerController>();
        CareerBrandsScaffoldBuilder.Build();
        Check(menuRoot.GetComponent<CareerBrandsView>().HasAuthoredLayout, "Toolbar builds the scene view");
        int menuObjects = GameObject.Created;
        var authoredTerminal = menuRoot.transform.Find("BrandsUI") as RectTransform; authoredTerminal.sizeDelta = new Vector2(1200, 800);
        CareerBrandsScaffoldBuilder.Build();
        Check(GameObject.Created == menuObjects && authoredTerminal.sizeDelta.x == 1200, "Running Build again preserves authored hierarchy and layout edits");
        UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode = true; CareerBrandsScaffoldBuilder.Build();
        Check(GameObject.Created == menuObjects, "Toolbar refuses Play Mode construction");
        UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode = false; UnityEditor.EditorUtility.Confirm = false; CareerBrandsScaffoldBuilder.Rebuild();
        Check(GameObject.Created == menuObjects, "Cancelling reset preserves authored layout");
        Console.WriteLine($"PASS: {checks} authored Brands UI hierarchy, binding, style-preservation and reveal checks");
    }
}
