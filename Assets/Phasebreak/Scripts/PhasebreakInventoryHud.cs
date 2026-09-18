using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(150), DisallowMultipleComponent]
    public sealed class PhasebreakInventoryHud : MonoBehaviour
    {
        private enum MenuMode { None, Inventory, Character, Talents, Loot }
        private static PhasebreakInventoryHud instance;
        private static readonly EquipmentSlot[] Slots = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));
        private PlayerBuildSystem build; private PlayerProgression progression; private PhasebreakFollowCamera cameraController; private Camera worldCamera;
        private InputAction inventoryAction, characterAction, talentsAction, escapeAction;
        private RectTransform canvasRect, inventoryPanel, inventoryGrid, characterPanel, equipmentGrid, talentsPanel, lootPanel, lootGrid, tooltip;
        private TextMeshProUGUI tooltipText, characterSummary, lootTitle, navTooltip;
        private readonly List<GameObject> inventoryCells = new(), equipmentCells = new(), lootCells = new();
        private readonly Dictionary<MenuMode, Image> navImages = new();
        private MenuMode mode; private CorpseLootContainer activeCorpse, hoveredCorpse;
        public static bool IsMajorMenuOpen => instance != null && instance.mode != MenuMode.None;
        public static bool IsLootWindowOpenFor(CorpseLootContainer corpse) => instance != null && instance.mode == MenuMode.Loot && instance.activeCorpse == corpse;
        public static void NotifyCorpseDespawned(CorpseLootContainer corpse) { if (IsLootWindowOpenFor(corpse)) instance.CloseMenu(); }

        private void Awake()
        {
            instance = this; build = FindAnyObjectByType<PlayerBuildSystem>(); progression = FindAnyObjectByType<PlayerProgression>();
            cameraController = FindAnyObjectByType<PhasebreakFollowCamera>(); worldCamera = Camera.main;
            inventoryAction = KeyAction("Inventory", "<Keyboard>/b"); characterAction = KeyAction("Character", "<Keyboard>/c"); talentsAction = KeyAction("Talents", "<Keyboard>/t"); escapeAction = KeyAction("Close Menu", "<Keyboard>/escape");
            EnsureEventSystem(); BuildUi(); CloseMenu();
        }
        private void OnEnable() { SetActions(true); if (build != null) build.BuildChanged += RefreshOpenPanel; }
        private void OnDisable() { SetActions(false); if (build != null) build.BuildChanged -= RefreshOpenPanel; CloseMenu(); }
        private void OnDestroy() { inventoryAction.Dispose(); characterAction.Dispose(); talentsAction.Dispose(); escapeAction.Dispose(); if (instance == this) instance = null; }
        private void Update()
        {
            if (inventoryAction.WasPressedThisFrame()) Toggle(MenuMode.Inventory);
            else if (characterAction.WasPressedThisFrame()) Toggle(MenuMode.Character);
            else if (talentsAction.WasPressedThisFrame()) Toggle(MenuMode.Talents);
            else if (escapeAction.WasPressedThisFrame() && mode != MenuMode.None) CloseMenu();
            UpdateCorpseInteraction();
        }
        public void OpenLoot(CorpseLootContainer corpse) { if (corpse == null) return; activeCorpse = corpse; Open(MenuMode.Loot); }
        private void Toggle(MenuMode target) { if (mode == target) CloseMenu(); else Open(target); }
        private void Open(MenuMode target)
        {
            mode = target; inventoryPanel.gameObject.SetActive(target == MenuMode.Inventory); characterPanel.gameObject.SetActive(target == MenuMode.Character);
            talentsPanel.gameObject.SetActive(target == MenuMode.Talents); lootPanel.gameObject.SetActive(target == MenuMode.Loot);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true; RefreshOpenPanel(); UpdateNav();
        }
        private void CloseMenu()
        {
            mode = MenuMode.None; activeCorpse = null;
            if (inventoryPanel != null) inventoryPanel.gameObject.SetActive(false); if (characterPanel != null) characterPanel.gameObject.SetActive(false);
            if (talentsPanel != null) talentsPanel.gameObject.SetActive(false); if (lootPanel != null) lootPanel.gameObject.SetActive(false); if (tooltip != null) tooltip.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true; UpdateNav();
        }

        private void BuildUi()
        {
            Canvas canvas = FindAnyObjectByType<PhasebreakHud>()?.GetComponentInChildren<Canvas>();
            if (canvas == null) { GameObject go = new("Phasebreak Menus", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; }
            canvasRect = canvas.GetComponent<RectTransform>(); BuildNavigation(canvasRect);
            inventoryPanel = Panel("Inventory Panel", canvasRect, new Vector2(.12f,.12f), new Vector2(.78f,.9f)); Header(inventoryPanel,"INVENTORY");
            Text("Bag Label", inventoryPanel,"RIFT SATCHEL",15,TextAlignmentOptions.Center,new Vector2(.03f,.84f),new Vector2(.72f,.91f));
            inventoryGrid = Block("Bag Grid",inventoryPanel,new Color(.025f,.04f,.065f,.95f)); Place(inventoryGrid,new Vector2(.03f,.08f),new Vector2(.72f,.84f),8);
            Text("Hint",inventoryPanel,"Left-click to equip",12,TextAlignmentOptions.Center,new Vector2(.03f,.02f),new Vector2(.72f,.075f));
            characterPanel = Panel("Character Panel",canvasRect,new Vector2(.08f,.08f),new Vector2(.92f,.93f)); Header(characterPanel,"CHARACTER");
            equipmentGrid = Block("Equipment Grid",characterPanel,new Color(.025f,.04f,.065f,.95f)); Place(equipmentGrid,new Vector2(.025f,.06f),new Vector2(.56f,.89f),8);
            RectTransform summaryPanel=Block("Character Analysis",characterPanel,new Color(.028f,.043f,.07f,.98f)); Place(summaryPanel,new Vector2(.58f,.06f),new Vector2(.975f,.89f),12);
            characterSummary=Text("Build Summary",summaryPanel,"",15,TextAlignmentOptions.TopLeft,Vector2.zero,Vector2.one); AddSpecButtons(summaryPanel);
            talentsPanel=Panel("Talents Panel",canvasRect,new Vector2(.25f,.25f),new Vector2(.75f,.75f)); Header(talentsPanel,"TALENTS");
            Text("Talent Placeholder",talentsPanel,"<size=28><b>TALENT MATRIX OFFLINE</b></size>\n\nThe navigation hook is ready.\nThe full talent tree arrives in a later phase.",18,TextAlignmentOptions.Center,new Vector2(.1f,.15f),new Vector2(.9f,.82f));
            lootPanel=Panel("Loot Panel",canvasRect,new Vector2(.31f,.27f),new Vector2(.69f,.78f)); lootTitle=Header(lootPanel,"CORPSE");
            lootGrid=Block("Corpse Grid",lootPanel,new Color(.025f,.04f,.065f,.96f)); Place(lootGrid,new Vector2(.06f,.2f),new Vector2(.94f,.82f),8);
            Button takeAll=ButtonWithText("Take All",lootPanel,"TAKE ALL",new Vector2(.31f,.05f),new Vector2(.69f,.16f)); takeAll.onClick.AddListener(TakeAll);
            tooltip=Block("Item Tooltip",canvasRect,new Color(.018f,.026f,.045f,.985f)); tooltip.sizeDelta=new Vector2(330,290); tooltip.anchorMin=tooltip.anchorMax=Vector2.zero; tooltip.pivot=new Vector2(0,1);
            tooltipText=Text("Text",tooltip,"",14,TextAlignmentOptions.TopLeft,Vector2.zero,Vector2.one); tooltipText.margin=new Vector4(14,12,14,12); tooltip.gameObject.SetActive(false);
        }
        private void BuildNavigation(RectTransform root)
        {
            RectTransform nav=Block("Gameplay Navigation",root,new Color(.02f,.03f,.05f,.9f)); nav.anchorMin=nav.anchorMax=new Vector2(1,0); nav.pivot=new Vector2(1,0); nav.anchoredPosition=new Vector2(-22,20); nav.sizeDelta=new Vector2(192,62);
            NavButton(nav,MenuMode.Inventory,"BAG",0,"Inventory [B]"); NavButton(nav,MenuMode.Character,"CHAR",1,"Character [C]"); NavButton(nav,MenuMode.Talents,"TAL",2,"Talents [T]");
            navTooltip=Text("Navigation Tooltip",root,"",13,TextAlignmentOptions.Center,new Vector2(.78f,.105f),new Vector2(.99f,.15f)); navTooltip.gameObject.SetActive(false);
        }
        private void NavButton(RectTransform root,MenuMode target,string glyph,int index,string hint)
        {
            RectTransform rt=Block(target.ToString(),root,new Color(.08f,.11f,.17f,1)); rt.anchorMin=rt.anchorMax=new Vector2(0,0); rt.pivot=new Vector2(0,0); rt.anchoredPosition=new Vector2(8+index*60,7); rt.sizeDelta=new Vector2(52,48);
            Button b=rt.gameObject.AddComponent<Button>(); Text("Icon",rt,glyph,13,TextAlignmentOptions.Center,Vector2.zero,Vector2.one); navImages[target]=rt.GetComponent<Image>();
            ItemSlotUI relay=rt.gameObject.AddComponent<ItemSlotUI>(); relay.Configure(()=>{navTooltip.text=hint;navTooltip.gameObject.SetActive(true);},()=>navTooltip.gameObject.SetActive(false),()=>Toggle(target));
        }

        private void RefreshOpenPanel()
        {
            if (mode==MenuMode.Inventory) RefreshInventory(); else if (mode==MenuMode.Character) RefreshCharacter(); else if (mode==MenuMode.Loot) RefreshLoot();
        }
        private void RefreshInventory()
        {
            Clear(inventoryCells); int capacity=Mathf.Max(24,build.Inventory.Count); for(int i=0;i<capacity;i++)
            { PhasebreakItemDefinition item=i<build.Inventory.Count?build.Inventory[i]:null; CreateItemCell(inventoryGrid,item,i,5,inventoryCells,item==null?null:()=>{build.Equip(item);RefreshInventory();},item==null?null:build.GetEquipped(item.slot),null); }
        }
        private void RefreshCharacter()
        {
            Clear(equipmentCells); for(int i=0;i<Slots.Length;i++) { EquipmentSlot slot=Slots[i]; PhasebreakItemDefinition item=build.GetEquipped(slot); CreateItemCell(equipmentGrid,item,i,4,equipmentCells,item==null?null:()=>{build.Unequip(slot);RefreshCharacter();},null,Pretty(slot)); }
            characterSummary.text="<b>CHARACTER STATS & BUILD</b>\n\n"+build.GetBuildSummary()+"\n\n<b>BUILD MODIFIERS</b>\n"+ModifierSummary();
        }
        private void RefreshLoot()
        {
            Clear(lootCells); if(activeCorpse==null){CloseMenu();return;} lootTitle.text=activeCorpse.DisplayName.ToUpperInvariant()+"  •  LOOT";
            int capacity=Mathf.Max(6,activeCorpse.Items.Count); for(int i=0;i<capacity;i++){PhasebreakItemDefinition item=i<activeCorpse.Items.Count?activeCorpse.Items[i]:null;CreateItemCell(lootGrid,item,i,3,lootCells,item==null?null:()=>{activeCorpse.Take(item,build);RefreshLoot();},item==null?null:build.GetEquipped(item.slot),null);}
            if(activeCorpse.IsEmpty) lootTitle.text=activeCorpse.DisplayName.ToUpperInvariant()+"  •  EMPTY";
        }
        private void TakeAll(){if(activeCorpse==null)return;activeCorpse.TakeAll(build);RefreshLoot();}
        private void CreateItemCell(RectTransform root,PhasebreakItemDefinition item,int index,int columns,List<GameObject> list,Action click,PhasebreakItemDefinition compare,string slotName)
        {
            const float cell=76,gap=10; int row=index/columns,col=index%columns; RectTransform rt=Block(item==null?"Empty Slot":item.displayName,root,item==null?new Color(.045f,.06f,.085f,.9f):RarityColor(item.rarity));
            rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(12+col*(cell+gap),-12-row*(cell+gap));rt.sizeDelta=new Vector2(cell,cell);list.Add(rt.gameObject);
            if(item!=null){RectTransform iconRt=Block("Icon",rt,new Color(1,1,1,.08f));Place(iconRt,new Vector2(.08f,.18f),new Vector2(.92f,.92f),0);Image icon=iconRt.GetComponent<Image>();if(item.icon!=null){icon.sprite=item.icon;icon.color=Color.white;icon.preserveAspect=true;}Text("Glyph",iconRt,item.icon!=null?"":Glyph(item.slot),24,TextAlignmentOptions.Center,Vector2.zero,Vector2.one);}
            if(!string.IsNullOrEmpty(slotName)) Text("Slot",rt,slotName,9,TextAlignmentOptions.Bottom,new Vector2(.02f,.01f),new Vector2(.98f,.22f));
            ItemSlotUI relay=rt.gameObject.AddComponent<ItemSlotUI>(); relay.Configure(()=>{if(item!=null)ShowTooltip(item,compare,rt);},HideTooltip,click);
        }
        private void ShowTooltip(PhasebreakItemDefinition item,PhasebreakItemDefinition compare,RectTransform source)
        {
            StringBuilder s=new();s.Append($"<size=19><b><color={RarityHex(item.rarity)}>{item.displayName}</color></b></size>\n{item.rarity} • iLvl {item.itemLevel} • {Pretty(item.slot)}\nTags: {item.tags}\n");
            Stat(s,"Power",item.stats.power,compare?.stats.power??0,true);Stat(s,"Health",item.stats.maxHealth,compare?.stats.maxHealth??0,false);Stat(s,"Defense",item.stats.defense,compare?.stats.defense??0,true);Stat(s,"Crit",item.stats.criticalChance,compare?.stats.criticalChance??0,true);Stat(s,"Crit Damage",item.stats.criticalDamage,compare?.stats.criticalDamage??0,true);Stat(s,"Attack Speed",item.stats.attackSpeed,compare?.stats.attackSpeed??0,true);Stat(s,"Mobility",item.stats.movementSpeed,compare?.stats.movementSpeed??0,true);Stat(s,"Boss Damage",item.stats.bossDamage,compare?.stats.bossDamage??0,true);
            if(item.itemSet!=null)s.Append($"\n<color=#72D4FF>Set: {item.itemSet.displayName}</color>");if(item.effects!=BuildEffect.None)s.Append($"\n<color=#C596FF>{item.effects}</color>");s.Append($"\n\n{item.description}");tooltipText.text=s.ToString();tooltip.gameObject.SetActive(true);
            Vector2 screen=RectTransformUtility.WorldToScreenPoint(null,source.position);RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,null,out Vector2 local);Rect rect=canvasRect.rect;local.x=Mathf.Clamp(local.x+48,rect.xMin+8,rect.xMax-tooltip.sizeDelta.x-8);local.y=Mathf.Clamp(local.y+40,rect.yMin+tooltip.sizeDelta.y+8,rect.yMax-8);tooltip.anchoredPosition=local;
        }
        private void HideTooltip()=>tooltip.gameObject.SetActive(false);
        private void UpdateCorpseInteraction()
        {
            if(mode!=MenuMode.None){SetHoveredCorpse(null);return;} if(worldCamera==null||Mouse.current==null)return;
            Vector2 point=cameraController!=null&&cameraController.RightClickStartedThisFrame?cameraController.RightClickPosition:Mouse.current.position.ReadValue(); Ray ray=worldCamera.ScreenPointToRay(point);
            CorpseLootContainer found=Physics.Raycast(ray,out RaycastHit hit,55f,~0,QueryTriggerInteraction.Collide)?hit.collider.GetComponentInParent<CorpseLootContainer>():null;SetHoveredCorpse(found);
            if(found!=null&&cameraController!=null&&cameraController.RightClickStartedThisFrame)OpenLoot(found);
        }
        private void SetHoveredCorpse(CorpseLootContainer corpse){if(hoveredCorpse==corpse)return;hoveredCorpse?.SetHovered(false);hoveredCorpse=corpse;hoveredCorpse?.SetHovered(true);}
        private string ModifierSummary()=> $"Crit → Energy: {build.CritEnergyRestore:0}\nPhase Lunge charges: {1+build.PhaseLungeExtraCharges}\nPhase Lunge cooldown: {build.PhaseLungeCooldownMultiplier:P0}\nCrushing cleave: {(build.CrushingBlowCleave?"Active":"Inactive")}\nTeleport recovery: {build.TeleportKillRecovery:0}";
        private void AddSpecButtons(RectTransform root){float x=.04f;foreach(Specialization spec in new[]{Specialization.Berserker,Specialization.Bulwark,Specialization.Riftblade}){Button b=ButtonWithText(spec.ToString(),root,spec.ToString(),new Vector2(x,.02f),new Vector2(x+.28f,.085f));b.onClick.AddListener(()=>{if(build.ChooseSpecialization(spec))RefreshCharacter();});x+=.3f;}}
        private void UpdateNav(){foreach(var pair in navImages)pair.Value.color=mode==pair.Key?new Color(.2f,.55f,.85f,1):new Color(.08f,.11f,.17f,1);}
        private void SetActions(bool enabled){foreach(InputAction a in new[]{inventoryAction,characterAction,talentsAction,escapeAction})if(enabled)a.Enable();else a.Disable();}
        private static InputAction KeyAction(string name,string binding)=>new(name,InputActionType.Button,binding);
        private static void EnsureEventSystem(){if(EventSystem.current==null)new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));}
        private static RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max){RectTransform p=Block(name,parent,new Color(.015f,.025f,.045f,.975f));Place(p,min,max,0);return p;}
        private static TextMeshProUGUI Header(Transform parent,string value)=>Text("Header",parent,value,23,TextAlignmentOptions.Center,new Vector2(0,.9f),Vector2.one);
        private static RectTransform Block(string name,Transform parent,Color color){GameObject go=new(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return go.GetComponent<RectTransform>();}
        private static TextMeshProUGUI Text(string name,Transform parent,string value,float size,TextAlignmentOptions align,Vector2 min,Vector2 max){GameObject go=new(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);TextMeshProUGUI t=go.GetComponent<TextMeshProUGUI>();t.text=value;t.fontSize=size;t.alignment=align;t.color=new Color(.88f,.93f,1);t.textWrappingMode=TextWrappingModes.Normal;Place(t.rectTransform,min,max,5);t.raycastTarget=false;return t;}
        private static Button ButtonWithText(string name,Transform parent,string value,Vector2 min,Vector2 max){RectTransform rt=Block(name,parent,new Color(.1f,.16f,.24f,1));Place(rt,min,max,0);Button b=rt.gameObject.AddComponent<Button>();Text("Label",rt,value,13,TextAlignmentOptions.Center,Vector2.zero,Vector2.one);return b;}
        private static void Place(RectTransform rt,Vector2 min,Vector2 max,float inset){rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=new Vector2(inset,inset);rt.offsetMax=new Vector2(-inset,-inset);}
        private static void Clear(List<GameObject> list){foreach(GameObject go in list)if(go!=null)Destroy(go);list.Clear();}
        private static string Pretty(EquipmentSlot slot)=>System.Text.RegularExpressions.Regex.Replace(slot.ToString(),"([a-z])([A-Z0-9])","$1 $2");
        private static string Glyph(EquipmentSlot slot)=>slot switch{EquipmentSlot.PrimaryWeapon=>"PW",EquipmentSlot.Secondary=>"SW",EquipmentSlot.Head=>"HD",EquipmentSlot.Shoulders=>"SH",EquipmentSlot.Chest=>"CH",EquipmentSlot.Hands=>"HN",EquipmentSlot.Legs=>"LG",EquipmentSlot.Boots=>"BT",EquipmentSlot.Core=>"CO",EquipmentSlot.WildcardArtifact=>"AR",EquipmentSlot.MobilityRelic=>"MR",EquipmentSlot.PowerRelic=>"PR",EquipmentSlot.UtilityRelic=>"UR",_=>"SG"};
        private static Color RarityColor(ItemRarity rarity)=>rarity switch{ItemRarity.Mythic=>new Color(.5f,.16f,.08f,1),ItemRarity.Epic=>new Color(.27f,.12f,.46f,1),ItemRarity.Rare=>new Color(.08f,.24f,.48f,1),ItemRarity.Uncommon=>new Color(.08f,.3f,.17f,1),_=>new Color(.12f,.14f,.18f,1)};
        private static string RarityHex(ItemRarity rarity)=>rarity switch{ItemRarity.Mythic=>"#FF784E",ItemRarity.Epic=>"#B58CFF",ItemRarity.Rare=>"#4BA3FF",ItemRarity.Uncommon=>"#62D77B",_=>"#D6D9DE"};
        private static void Stat(StringBuilder s,string name,float value,float old,bool percent){if(Mathf.Approximately(value,0))return;float d=value-old;string c=d>.001f?"#62E68A":d<-.001f?"#FF6670":"#C9D2E3";s.Append($"\n<color={c}>{name}: {(percent?value.ToString("+0%;-0%") : value.ToString("+0;-0"))}</color>");}
    }
}
