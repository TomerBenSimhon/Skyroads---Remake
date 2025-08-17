using System;
using UnityEngine;

[Serializable] public abstract class BaseSpec { }
[Serializable] public class FooSpec : BaseSpec { public float foo = 1f; }

public class RefTest : MonoBehaviour
{
    [SerializeReference] public BaseSpec single;
    [SerializeReference, NonReorderable] public System.Collections.Generic.List<BaseSpec> list = new();
}