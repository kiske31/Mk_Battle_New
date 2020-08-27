using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuickPool;

public class Arrow : MonoBehaviour
{
    private void OnEnable()
    {
        Invoke("SelfDespawn", 3); // 3초뒤에 자신을 디스폰 시킵니다
    }

    void SelfDespawn()
    {
        PoolsManager.Despawn(this.gameObject);
    }
}
