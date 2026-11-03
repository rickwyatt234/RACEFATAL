using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RaceFatal.Presentation.Career;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
class Program
{
 static int checks;
 static void Check(bool value,string why){if(!value)throw new Exception(why);checks++;}
 static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static void Main()
 {
  var root=new GameObject("List");var page=root.AddComponent<AuthoredPage>();
  var rows=Enumerable.Range(0,3).Select(i=>new GameObject("Row "+i).AddComponent<TMP_Text>()).ToArray();
  var back=new GameObject("Previous").AddComponent<Button>();var next=new GameObject("Next").AddComponent<Button>();var label=new GameObject("Page").AddComponent<TMP_Text>();
  Set(page,"rows",rows.Cast<Component>().ToArray());Set(page,"previous",back);Set(page,"next",next);Set(page,"pageLabel",label);
  int objects=GameObject.Created,components=GameObject.ComponentsAdded;
  void Load(int count){page.Begin();for(int i=0;i<count;i++){int capture=i;page.Add(c=>((TMP_Text)c).text="Item "+capture);}page.Render();}
  Load(0);Check(rows.All(r=>!r.gameObject.activeSelf)&&!next.interactable&&!back.interactable,"Empty list");
  Load(10001);Check(rows[0].text=="Item 0"&&next.interactable&&!back.interactable,"First page");
  var seen=new HashSet<string>();
  while(true){foreach(var row in rows.Where(r=>r.gameObject.activeSelf))seen.Add(row.text);if(!next.interactable)break;next.onClick.Invoke();}
  Check(seen.Count==10001,"Every item remains reachable without instantiation or a collection cap");
  Check(rows[0].text=="Item 9999"&&rows[1].text=="Item 10000"&&!rows[2].gameObject.activeSelf,"Partial last page");
  back.onClick.Invoke();Check(rows[0].text=="Item 9996","Previous page");
  Load(2);Check(rows[0].text=="Item 0"&&!next.interactable&&!back.interactable,"Collection shrink clamps page");
  Load(0);Check(rows.All(r=>!r.gameObject.activeSelf),"No stale rows after clear");
  rows[0].fontSize=37;Load(7);Check(rows[0].fontSize==37,"Designer styling survives refresh");
  for(int i=0;i<10;i++)Load(7);
  Check(next.onClick.Count==1&&back.onClick.Count==1,"Repeated refresh does not duplicate callbacks");
  Check(GameObject.Created==objects&&GameObject.ComponentsAdded==components,"No runtime UI allocation");
  bool missing=false;try{AuthoredPage.Require(new GameObject("Missing").AddComponent<TMP_Text>());}catch(InvalidOperationException){missing=true;}
  Check(missing,"Missing authored layout fails explicitly");
  Pause();Audit();Console.WriteLine($"PASS: {checks} UI pagination, authored binding, parser and runtime-source checks");
 }
 static void Pause()
 {
  var root=new GameObject("Pause");var view=root.AddComponent<RaceFatal.Presentation.Racing.RacePauseView>();
  var group=root.AddComponent<CanvasGroup>();var menu=new GameObject("Menu");var confirm=new GameObject("Confirm");
  Button Make(string name){var obj=new GameObject(name);obj.AddComponent<TMP_Text>();return obj.AddComponent<Button>();}
  var resume=Make("Resume");var hide=Make("Hide");var quit=Make("Quit");var cancel=Make("Cancel");var accept=Make("Accept");
  var hints=new GameObject("Hints").AddComponent<TMP_Text>();
  Set(view,"group",group);Set(view,"menu",menu);Set(view,"confirmation",confirm);Set(view,"resumeButton",resume);Set(view,"hideButton",hide);Set(view,"quitButton",quit);Set(view,"cancelButton",cancel);Set(view,"confirmQuitButton",accept);Set(view,"hints",hints);
  var controller=new RaceFatal.Presentation.Racing.RacePauseController();
  UnityEngine.EventSystems.EventSystem.current=new();
  int objects=GameObject.Created,components=GameObject.ComponentsAdded;
  view.Initialize(controller,KeyCode.Escape,KeyCode.F1);Check(!view.IsVisible,"Pause initially hidden");
  view.ShowMenu();Check(view.IsVisible&&!view.IsConfirmingQuit&&group.interactable,"Show authored pause controls");
  Check(UnityEngine.EventSystems.EventSystem.current.selected==resume.gameObject,"Resume receives focus");
  quit.onClick.Invoke();Check(view.IsConfirmingQuit&&!menu.activeSelf,"Quit opens saved confirmation");
  Check(UnityEngine.EventSystems.EventSystem.current.selected==cancel.gameObject,"Quit confirmation defaults to safe back button");
  cancel.onClick.Invoke();Check(!view.IsConfirmingQuit&&menu.activeSelf&&controller.quit==0,"Cancel does not quit");
  view.HideMenu();Check(!view.IsVisible&&!group.interactable,"Screenshot mode hides menu");
  view.ShowMenu();resume.onClick.Invoke();hide.onClick.Invoke();Check(controller.resumed==1&&controller.toggled==1,"Runtime callbacks bound");
  view.Initialize(controller,KeyCode.Escape,KeyCode.F1);resume.onClick.Invoke();Check(controller.resumed==2,"Reinitialization never duplicates callbacks");
  quit.onClick.Invoke();accept.onClick.Invoke();Check(controller.quit==1,"Confirmed quit invokes controller once");
  view.Close();Check(!view.IsVisible,"Close preserves saved objects");
  Check(GameObject.Created==objects&&GameObject.ComponentsAdded==components,"Pause creates no UI during repeated use");
 }
 static void Audit()
 {
  string repo=Directory.GetCurrentDirectory();while(!Directory.Exists(Path.Combine(repo,"Assets","Game")))repo=Directory.GetParent(repo).FullName;
  var scripts=Directory.GetFiles(Path.Combine(repo,"Assets","Game"),"*.cs",SearchOption.AllDirectories);
  var uiFiles=new HashSet<string>(new[]{"CareerHomeCalendarView.cs","CareerPreparationView.cs","CareerSuccessionView.cs","CareerHomeView.cs","CareerShopView.cs","CareerGarageView.cs","CareerRosterView.cs","CareerRacesView.cs","CareerBrandsView.cs","AuthoredPage.cs","UiBindings.cs","EditorUiFactory.cs","TeamCreationController.cs","TeamColorPicker.cs","ColorPickerPointer.cs","CampaignReviewController.cs","RacePauseView.cs","RacePauseController.cs","TacticalRaceUI.cs","PlayerCockpitHUD.cs","CockpitAudienceHUD.cs","PlayerCockpitTargetingHUD.cs","PlayerWeaponAim.cs"});
  int menus=0;
  foreach(var file in scripts)
  {
   string text=File.ReadAllText(file);
   foreach(bool editor in new[]{false,true})
   {
    var tree=CSharpSyntaxTree.ParseText(text,new CSharpParseOptions(preprocessorSymbols:editor?new[]{"UNITY_EDITOR"}:Array.Empty<string>()),file);
    var errors=tree.GetDiagnostics().Where(d=>d.Severity==DiagnosticSeverity.Error).ToArray();
    Check(errors.Length==0,string.Join("\n",errors.Select(e=>e.ToString())));
    if(editor){menus+=tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Count(a=>a.Name.ToString()=="MenuItem");continue;}
    if(!uiFiles.Contains(Path.GetFileName(file)))continue;
    foreach(var creation in tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
     Check(creation.Type.ToString()!="GameObject",file+": runtime GameObject creation");
    foreach(var call in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
    {
     var name=call.Expression.ToString();
     Check(!name.Contains("AddComponent")&&!name.Contains("Instantiate")&&!name.Contains("EditorUiFactory")&&!name.Contains("EditorAuthor")&&!name.Contains("BuildUI"),file+": runtime construction call "+name);
    }
   }
  }
  Check(menus==2,"Only the two Brands UI menu commands may remain");
  var migration=File.ReadAllText(Path.Combine(repo,"Assets/Game/Editor/UiAuthoringMigration.cs"));
  Check(!migration.Contains("BrandUiLayout")&&!migration.Contains("CareerBrandsScaffoldBuilder"),"Migration leaves Brands hierarchy to PR55's authoring tool");
 }
}
