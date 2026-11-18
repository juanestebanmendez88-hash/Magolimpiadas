using UnityEngine;

[CreateAssetMenu(menuName = "Magolimpiadas/Iconos de elementos", fileName = "IconosElementos")]
public class ElementsIcons : ScriptableObject
{
    public Sprite fire;
    public Sprite nebula;
    public Sprite plant;
    public Sprite lightning;

    public Sprite For(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return fire;
            case MagicElement.Nebula: return nebula;
            case MagicElement.Plant: return plant;
            case MagicElement.Lightning: return lightning;
        }
        return null;
    }

    static ElementsIcons loaded;
    static bool searched;

    public static ElementsIcons Load()
    {
        if (!searched) { loaded = Resources.Load<ElementsIcons>("IconosElementos"); searched = true; }
        return loaded;
    }
}
