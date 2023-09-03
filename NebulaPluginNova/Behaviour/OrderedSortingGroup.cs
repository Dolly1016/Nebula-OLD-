using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;

namespace Nebula.Behaviour;

public class ZOrderedSortingGroup : MonoBehaviour
{
    static ZOrderedSortingGroup() => ClassInjector.RegisterTypeInIl2Cpp<ZOrderedSortingGroup>();
    private SortingGroup group;
    public void Start()
    {
        group = gameObject.AddComponent<SortingGroup>();
    }

    private float rate = 2000f;
    private int baseValue = 5;
    public void Update()
    {
        group.sortingOrder = baseValue - (int)(rate * transform.localPosition.z);
    }
}
