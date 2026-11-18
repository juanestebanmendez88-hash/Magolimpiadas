using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Templo/Tile animado")]
public class AnimatedTileMap : TileBase
{
    public Sprite[] sprites;
    public float fps = 4f;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = sprites != null && sprites.Length > 0 ? sprites[0] : null;
        tileData.color = Color.white;
        tileData.flags = TileFlags.LockTransform;
        tileData.colliderType = Tile.ColliderType.None;
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (sprites == null || sprites.Length < 2) return false;
        tileAnimationData.animatedSprites = sprites;
        tileAnimationData.animationSpeed = fps;
        tileAnimationData.animationStartTime = 0f;
        return true;
    }
}
