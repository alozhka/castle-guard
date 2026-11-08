Follow the coding conventions:

1. Do not use `var`, use `target-typed new` initialization instead. 
    - Specify the type, when it is unobvious, like in constructors (`run = new LevelRun(...)`)
2. Use `camelCase` for private fields in classes derived from `UnityEngine.Object` (including `MonoBehaviour` and `ScriptableObject`) with `[SerializeField]`.
    - Use `_camelCase` for private fields in all other C# types, including tests.
3. Use trailing commas
4. Use file-scoped namespaces, except for types derived from `UnityEngine.Object` (including `MonoBehaviour` and `ScriptableObject`), which require block-scoped namespaces.
