using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "RandomTile", menuName = "2D/Tiles/RandomTile")]
public class RandomTile : Tile
{
    [Header("섞어서 찍을 타일 이미지들을 넣으세요")]
    public Sprite[] sprites;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);

        if (sprites != null && sprites.Length > 0)
        {
            // 1. 좌표값을 이용해 고유한 시드값 생성
            int seed = position.x * 100000 + position.y;
            Random.InitState(seed);

            // 2. 무작위 타일 이미지 뽑기
            tileData.sprite = sprites[Random.Range(0, sprites.Length)];

            // 3. 변환 초기화
            tileData.transform = Matrix4x4.identity;

            // LockTransform을 해주면 엔진이나 다른 브러시가 타일을 마음대로 돌리는 것도 막아줍니다.
            tileData.flags = TileFlags.LockTransform;
        }
    }
}