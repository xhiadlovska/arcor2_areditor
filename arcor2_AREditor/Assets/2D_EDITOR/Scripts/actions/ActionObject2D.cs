using UnityEngine;


public class ActionObject2D : Base.ActionObject {


    private Vector3 offset;

    private void Touch() {
        MenuManager.Instance.ShowMenu(InteractiveObjectMenu, Data.Id);
        InteractiveObjectMenu.GetComponent<InteractiveObjectMenu>().CurrentObject = gameObject;
    }

    private void OnMouseDown() => offset = gameObject.transform.position -
            Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10.0f));

    private void OnMouseDrag() {
        Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10.0f);
        transform.position = Camera.main.ScreenToWorldPoint(newPosition) + offset;
    }

    private void OnMouseUp() {
        GameManager.Instance.UpdateScene();
    }

    public override Vector3 GetScenePosition() {
        Vector3 v = GameManager.Instance.Scene.transform.TransformPoint(Vector3.Scale(DataHelper.PositionToVector3(Data.Pose.Position), new Vector3(1000, 1000, 1)) -
             new Vector3(GameManager.Instance.Scene.GetComponent<RectTransform>().rect.width / 2, GameManager.Instance.Scene.GetComponent<RectTransform>().rect.height / 2, 0));
        return v;
    }

    public override void SetScenePosition(Vector3 position) {
        
        Data.Pose.Position = DataHelper.Vector3ToPosition(Vector3.Scale(GameManager.Instance.Scene.transform.InverseTransformPoint(transform.position) +
            new Vector3(GameManager.Instance.Scene.GetComponent<RectTransform>().rect.width / 2, GameManager.Instance.Scene.GetComponent<RectTransform>().rect.height / 2, 0), new Vector3(0.001f, 0.001f, 1)));
    }

    protected override void Update() {        
        base.Update();
    }

    public override Quaternion GetSceneOrientation() {
        return DataHelper.OrientationToQuaternion(Data.Pose.Orientation);
    }

    public override void SetSceneOrientation(Quaternion orientation) {
        Data.Pose.Orientation = DataHelper.QuaternionToOrientation(orientation);
    }
}




