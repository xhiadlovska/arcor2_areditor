using Base;
using RuntimeGizmos;
using UnityEngine;
using System.Collections.Generic;
using IO.Swagger.Model;
using TMPro;
using System;
using System.Threading.Tasks;
using NativeWebSocket;

[RequireComponent(typeof(OutlineOnClick))]
public class ActionPoint3D : Base.ActionPoint {

    public GameObject Sphere, Visual, CollapsedPucksVisual, Lock;
    public TextMeshPro ActionPointName;
    private Material sphereMaterial;

    private bool manipulationStarted = false;

    private float interval = 0.1f;
    private float nextUpdate = 0;

    private bool updatePosition = false;
    [SerializeField]
    private OutlineOnClick outlineOnClick;


    private void Awake() {
    }


    protected override async void Update() {
        if (manipulationStarted) {
            if (TransformGizmo.Instance.mainTargetRoot != null && GameObject.ReferenceEquals(TransformGizmo.Instance.mainTargetRoot.gameObject, Sphere)) {
                if (!TransformGizmo.Instance.isTransforming && updatePosition) {
                    updatePosition = false;
                    UpdatePose();
                }

                if (TransformGizmo.Instance.isTransforming)
                    updatePosition = true;


            } else {
                manipulationStarted = false;
                if (!await this.WriteUnlock())
                    return;
            }
        }
            
        //TODO shouldn't this be called first?
        base.Update();
    }

    private async void UpdatePose() {
        try {
            await WebsocketManager.Instance.UpdateActionPointPosition(Data.Id, Data.Position);
        } catch (RequestFailedException e) {
            Notifications.Instance.ShowNotification("Failed to update action point position", e.Message);
            ResetPosition();
        }
    }

    private void LateUpdate() {
        // Fix of AP rotations - works on both PC and tablet
        transform.rotation = Base.SceneManager.Instance.SceneOrigin.transform.rotation;
        if (Parent != null)
            orientations.transform.rotation = Parent.GetTransform().rotation;
        else
            orientations.transform.rotation = Base.SceneManager.Instance.SceneOrigin.transform.rotation;
    }

    public override void OnClick(Click type) {
        if (!enabled || !Enabled)
            return;
        if (GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.SelectingActionPoint ||
            GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.SelectingActionPointParent) {
            GameManager.Instance.ObjectSelected(this);
            return;
        }
        if (GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.Normal) {
            return;
        }
        if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor) {
            Notifications.Instance.ShowNotification("Not allowed", "Editation of action point only allowed in project editor");
            return;
        }

        TransformGizmo.Instance.ClearTargets();
        outlineOnClick.GizmoUnHighlight();
        // HANDLE MOUSE
        if (type == Click.MOUSE_LEFT_BUTTON || type == Click.LONG_TOUCH) {
            StartManipulation();            
        } else if (type == Click.MOUSE_RIGHT_BUTTON || type == Click.TOUCH) {
            outlineOnClick.GizmoUnHighlight();
        }

    }

    public async void ShowMenu(bool enableBackButton = false) {
        if (!await this.WriteLock(false))
            return;

        actionPointMenu.CurrentActionPoint = this;
        actionPointMenu.EnableBackButton(enableBackButton);
        MenuManager.Instance.ShowMenu(MenuManager.Instance.ActionPointMenu);
    }


    public override Vector3 GetScenePosition() {
        return TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Position));
    }

    public override void SetScenePosition(Vector3 position) {
        Data.Position = DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(position));
    }

    public override Quaternion GetSceneOrientation() {
        //return TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(Data.Orientations[0].Orientation));
        return new Quaternion();
    }

    public override void SetSceneOrientation(Quaternion orientation) {
        //Data.Orientations.Add(new IO.Swagger.Model.NamedOrientation(id: "default", orientation:DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(orientation))));
    }

    public override void UpdatePositionsOfPucks() {
        CollapsedPucksVisual.SetActive(ProjectManager.Instance.AllowEdit && ActionsCollapsed);
        if (ProjectManager.Instance.AllowEdit && ActionsCollapsed) {
            foreach (Action3D action in Actions.Values) {
                action.transform.localPosition = new Vector3(0, 0, 0);
                action.transform.localScale = new Vector3(0, 0, 0);
            }
            
        } else {
            int i = 1;
            foreach (Action3D action in Actions.Values) {
                action.transform.localPosition = new Vector3(0, i * 0.03f, 0);
                ++i;
                action.transform.localScale = new Vector3(1, 1, 1);
            }
        }        
    }
    
    public override bool ProjectInteractable() {
        return base.ProjectInteractable() && !MenuManager.Instance.IsAnyMenuOpened;
    }

    public override void ActivateForGizmo(string layer) {
        if (!Locked) {
            base.ActivateForGizmo(layer);
            Sphere.layer = LayerMask.NameToLayer(layer);
        }
    }

    /// <summary>
    /// Changes size of shpere representing action point
    /// </summary>
    /// <param name="size"><0; 1> - 0 means invisble, 1 means 10cm in diameter</param>
    public override void SetSize(float size) {
        Visual.transform.localScale = new Vector3(size / 10, size / 10, size / 10);
    }

    public override (List<string>, Dictionary<string, string>) UpdateActionPoint(IO.Swagger.Model.ActionPoint projectActionPoint) {
        (List<string>, Dictionary<string, string>) result = base.UpdateActionPoint(projectActionPoint);
        ActionPointName.text = projectActionPoint.Name;
        return result;
    }

    public override void UpdateOrientation(NamedOrientation orientation) {
        base.UpdateOrientation(orientation);
    }

    public override void AddOrientation(NamedOrientation orientation) {
        base.AddOrientation(orientation);
    }

    public override void HighlightAP(bool highlight) {
        if (highlight) {
            outlineOnClick.Highlight();
        } else {
            outlineOnClick.UnHighlight();
        }
    }

    public override void OnHoverStart() {
        if (!enabled)
            return;
        if (GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.Normal &&
            GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.SelectingActionPoint &&
            GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.SelectingActionPointParent) {
            if (GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.InteractionDisabled) {
                if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.PackageRunning)
                    return;
            } else {
                return;
            }
        }
        if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor &&
            GameManager.Instance.GetGameState() != GameManager.GameStateEnum.PackageRunning) {
            return;
        }
        
        HighlightAP(true);
        ActionPointName.gameObject.SetActive(true);
        if (Locked)
            Lock.SetActive(true);
    }

    public override void OnHoverEnd() {
        HighlightAP(false);
        ActionPointName.gameObject.SetActive(false);
        Lock.SetActive(false);
    }


    public override void ActionPointBaseUpdate(IO.Swagger.Model.BareActionPoint apData) {
        base.ActionPointBaseUpdate(apData);
        ActionPointName.text = apData.Name;
    }

    public override void InitAP(IO.Swagger.Model.ActionPoint apData, float size, IActionPointParent parent = null) {
        base.InitAP(apData, size, parent);
        sphereMaterial = Sphere.GetComponent<Renderer>().material;
        ActionPointName.text = apData.Name;
    }

    public override void UpdateColor() {
        if (Enabled && !IsLocked)
            sphereMaterial.color = new Color(0.51f, 0.51f, 0.89f);
        else
            sphereMaterial.color = Color.gray;
    }

    public override async void OpenMenu() {
        ShowMenu();
    }

    public override bool HasMenu() {
        return true;
    }

    public async override void StartManipulation() {
        if (!await this.WriteLock(true))
            return;
        TransformGizmo.Instance.ClearTargets();
        if (Locked) {
            Notifications.Instance.ShowNotification("Locked", "This action point is locked and can't be manipulated");
        } else {

            try {
                await WebsocketManager.Instance.UpdateActionPointPosition(Data.Id, new Position(), true);
                // We have clicked with left mouse and started manipulation with object
                manipulationStarted = true;
                updatePosition = false;
                TransformGizmo.Instance.AddTarget(Sphere.transform);
                outlineOnClick.GizmoHighlight();
            } catch (RequestFailedException ex) {
                Notifications.Instance.ShowNotification("Action point pose could not be changed", ex.Message);
            }
        }
    }

    internal GameObject GetModelCopy() {
        GameObject sphere = Instantiate(Sphere);
        Destroy(sphere.GetComponent<SphereCollider>());
        sphere.transform.localScale = Visual.transform.localScale;
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localRotation = Quaternion.identity;
        return sphere;
    }

    public async override Task<RequestResult> Removable() {
        if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor)
            return new RequestResult(false, "AP could only be removed in project editor");
        else if (Locked) {
            return new RequestResult(false, "AP is locked");
        } else {
            try {
                await WebsocketManager.Instance.RemoveActionPoint(GetId(), true);
                return new RequestResult(true);
            } catch (RequestFailedException ex) {
                return new RequestResult(false, ex.Message);
            }
        }
    }

    public async override void Remove() {
        try {
            await WebsocketManager.Instance.RemoveActionPoint(GetId(), false);
        } catch (RequestFailedException ex) {
            Notifications.Instance.ShowNotification("Failed to remove AP " + GetName(), ex.Message);
        }
    }

    public async override Task Rename(string name) {
        try {
            await WebsocketManager.Instance.RenameActionPoint(GetId(), name);
            Notifications.Instance.ShowToastMessage("Action point renamed");
        } catch (RequestFailedException e) {
            Notifications.Instance.ShowNotification("Failed to rename action point", e.Message);
            throw;
        }
    }

    public override string GetObjectTypeName() {
        return "Action point";
    }

    public override void OnObjectLocked(string owner) {
        base.OnObjectLocked(owner);
        if (owner != LandingScreen.Instance.GetUsername())
            ActionPointName.text = GetLockedText();
    }

    public override void OnObjectUnlocked() {
        base.OnObjectUnlocked();
        ActionPointName.text = GetName();
    }
}
