using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class FallingRockManager : MonoBehaviour
{
    [SerializeField] private FallingRock rockPrefab;
    [SerializeField] private float minSpawnInterval = 2.0f;
    [SerializeField] private float maxSpawnInterval = 3.0f;
    [SerializeField] private List<Transform> spawnPoints;

    private IObjectPool<FallingRock> pool;

    // 여러 개의 코루틴을 관리하기 위해 List로 변경
    private List<Coroutine> spawnCoroutines = new List<Coroutine>();

    private void Awake()
    {
        InitializePool();
    }

    private void OnEnable()
    {
        // 코루틴 중복 실행 방지 (이미 실행 중인 코루틴이 없을 때만)
        if (spawnCoroutines.Count == 0)
        {
            // 각 스폰 포인트마다 개별적인 코루틴 실행
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Coroutine c = StartCoroutine(SpawnRoutine(spawnPoint));
                    spawnCoroutines.Add(c);
                }
            }
        }
    }

    private void OnDisable()
    {
        // 모든 스폰 코루틴 정지
        foreach (Coroutine c in spawnCoroutines)
        {
            if (c != null)
            {
                StopCoroutine(c);
            }
        }
        spawnCoroutines.Clear(); // 리스트 비우기

        // 씬에 떠 있는 모든 낙석을 찾아 강제 반환/제거
        if (pool != null)
        {
            pool.Clear();
        }
    }

    private void InitializePool()
    {
        pool = new ObjectPool<FallingRock>(
            () => Instantiate(rockPrefab),
            (rock) => rock.gameObject.SetActive(true),
            (rock) => rock.gameObject.SetActive(false),
            (rock) => {
                if (rock != null)
                {
                    Destroy(rock.gameObject);
                }
            },
        true, 10, 20
        );
    }

    // 각 코루틴이 자신만의 스폰 포인트를 전달받아 독립적으로 타이머를 돌림
    private IEnumerator SpawnRoutine(Transform mySpawnPoint)
    {
        yield return new WaitForSeconds(Random.Range(0f, maxSpawnInterval));

        while (true)
        {
            // 각자 지정된 간격만큼 대기
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));

            // 풀에서 가져오기
            var rock = pool.Get();

            // 전달받은 개별 스폰 포인트의 위치로 설정
            rock.transform.position = mySpawnPoint.position;
            rock.SetPool(pool);
        }
    }
}