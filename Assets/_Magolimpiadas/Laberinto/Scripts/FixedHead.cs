using UnityEngine;

public class FixedHead : MonoBehaviour
{
    Transform root, neck, head;
    Quaternion neckRot, headRot;

    public static void Put(Animator anim)
    {
        if (anim == null || !anim.isHuman) return;
        var f = anim.GetComponent<FixedHead>();
        if (f == null) f = anim.gameObject.AddComponent<FixedHead>();
        f.root = anim.transform;
        f.neck = anim.GetBoneTransform(HumanBodyBones.Neck);
        f.head = anim.GetBoneTransform(HumanBodyBones.Head);
        var inv = Quaternion.Inverse(f.root.rotation);
        if (f.neck != null) f.neckRot = inv * f.neck.rotation;
        if (f.head != null) f.headRot = inv * f.head.rotation;
    }

    public static void Remove(Animator anim)
    {
        if (anim == null) return;
        var f = anim.GetComponent<FixedHead>();
        if (f != null) Destroy(f);
    }

    void LateUpdate()
    {
        if (root == null) return;
        if (neck != null) neck.rotation = root.rotation * neckRot;
        if (head != null) head.rotation = root.rotation * headRot;
    }
}
