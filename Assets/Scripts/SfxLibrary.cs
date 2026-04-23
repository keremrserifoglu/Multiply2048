using UnityEngine;

public enum SfxId
{
    MergeCrack,
    MergeBody,
    Merge2048Sparkle,
    Merge2048Air,
    GameOverClose,
    GameOverHope,
    MenuModeSelect,
    ButtonClick,
    Hint
}

[CreateAssetMenu(menuName = "Audio/SfxLibrary")]
public class SfxLibrary : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public SfxId id;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(-0.2f, 0.2f)] public float pitchJitter = 0.06f;
    }

    public Entry[] entries;

    public bool TryGet(SfxId id, out Entry entry)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].id == id)
                {
                    entry = entries[i];
                    return true;
                }
            }
        }

        entry = null;
        return false;
    }
}
