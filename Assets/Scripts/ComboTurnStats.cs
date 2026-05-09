using UnityEngine;

[System.Serializable]
public sealed class ComboTurnStats
{
    [SerializeField] private bool comboDetected;
    [SerializeField] private bool rewardDetected;
    [SerializeField] private int comboMergeCountValue;
    [SerializeField] private int rewardCountValue;
    [SerializeField] private int highestSourceValueValue;
    [SerializeField] private int highestMergedValueValue;
    [SerializeField] private Vector3 rewardWorldPositionValue;
    [SerializeField] private Vector3 popupWorldPositionValue;
    [SerializeField] private Vector3 moveDirectionValue = Vector3.up;
    [SerializeField] private Quaternion rotationOffsetValue = Quaternion.identity;
    [SerializeField] private Camera cameraForPixelsValue;

    public bool hasCombo { get => comboDetected; set => comboDetected = value; }
    public bool HasCombo { get => comboDetected; set => comboDetected = value; }
    public bool didCombo { get => comboDetected; set => comboDetected = value; }
    public bool DidCombo { get => comboDetected; set => comboDetected = value; }
    public bool comboTriggered { get => comboDetected; set => comboDetected = value; }
    public bool ComboTriggered { get => comboDetected; set => comboDetected = value; }

    public bool hasReward { get => rewardDetected; set => rewardDetected = value; }
    public bool HasReward { get => rewardDetected; set => rewardDetected = value; }
    public bool didReward { get => rewardDetected; set => rewardDetected = value; }
    public bool DidReward { get => rewardDetected; set => rewardDetected = value; }
    public bool rewardTriggered { get => rewardDetected; set => rewardDetected = value; }
    public bool RewardTriggered { get => rewardDetected; set => rewardDetected = value; }

    public int comboMergeCount { get => comboMergeCountValue; set => comboMergeCountValue = Mathf.Max(0, value); }
    public int ComboMergeCount { get => comboMergeCountValue; set => comboMergeCountValue = Mathf.Max(0, value); }
    public int eligibleMergeCount { get => comboMergeCountValue; set => comboMergeCountValue = Mathf.Max(0, value); }
    public int EligibleMergeCount { get => comboMergeCountValue; set => comboMergeCountValue = Mathf.Max(0, value); }

    public int rewardCount { get => rewardCountValue; set => rewardCountValue = Mathf.Max(0, value); }
    public int RewardCount { get => rewardCountValue; set => rewardCountValue = Mathf.Max(0, value); }

    public int highestSourceValue { get => highestSourceValueValue; set => highestSourceValueValue = Mathf.Max(highestSourceValueValue, value); }
    public int HighestSourceValue { get => highestSourceValueValue; set => highestSourceValueValue = Mathf.Max(highestSourceValueValue, value); }

    public int highestMergedValue { get => highestMergedValueValue; set => highestMergedValueValue = Mathf.Max(highestMergedValueValue, value); }
    public int HighestMergedValue { get => highestMergedValueValue; set => highestMergedValueValue = Mathf.Max(highestMergedValueValue, value); }

    public Vector3 rewardWorldPosition { get => rewardWorldPositionValue; set => rewardWorldPositionValue = value; }
    public Vector3 RewardWorldPosition { get => rewardWorldPositionValue; set => rewardWorldPositionValue = value; }

    public Vector3 popupWorldPosition { get => popupWorldPositionValue; set => popupWorldPositionValue = value; }
    public Vector3 PopupWorldPosition { get => popupWorldPositionValue; set => popupWorldPositionValue = value; }

    public Vector3 worldPosition
    {
        get => rewardWorldPositionValue;
        set
        {
            rewardWorldPositionValue = value;
            popupWorldPositionValue = value;
        }
    }

    public Vector3 WorldPosition
    {
        get => worldPosition;
        set => worldPosition = value;
    }

    public Vector3 moveDirection { get => moveDirectionValue; set => moveDirectionValue = value.sqrMagnitude > 0.0001f ? value.normalized : Vector3.up; }
    public Vector3 MoveDirection { get => moveDirectionValue; set => moveDirection = value; }

    public Quaternion rotationOffset { get => rotationOffsetValue; set => rotationOffsetValue = value; }
    public Quaternion RotationOffset { get => rotationOffsetValue; set => rotationOffsetValue = value; }

    public Camera cameraForPixels { get => cameraForPixelsValue; set => cameraForPixelsValue = value; }
    public Camera CameraForPixels { get => cameraForPixelsValue; set => cameraForPixelsValue = value; }

    public bool HasAnyReward => rewardCountValue > 0 || rewardDetected;
    public bool HasAnyCombo => comboMergeCountValue > 0 || comboDetected;

    public void Reset()
    {
        comboDetected = false;
        rewardDetected = false;
        comboMergeCountValue = 0;
        rewardCountValue = 0;
        highestSourceValueValue = 0;
        highestMergedValueValue = 0;
        rewardWorldPositionValue = Vector3.zero;
        popupWorldPositionValue = Vector3.zero;
        moveDirectionValue = Vector3.up;
        rotationOffsetValue = Quaternion.identity;
        cameraForPixelsValue = null;
    }

    public void RegisterMerge(int sourceValue, int mergedValue, Vector3 worldPositionValue, int minSourceValue, int rewardMergedValue)
    {
        highestSourceValueValue = Mathf.Max(highestSourceValueValue, sourceValue);
        highestMergedValueValue = Mathf.Max(highestMergedValueValue, mergedValue);

        if (sourceValue < minSourceValue)
            return;

        comboDetected = true;
        comboMergeCountValue++;
        worldPosition = worldPositionValue;

        if (mergedValue >= rewardMergedValue)
        {
            rewardDetected = true;
            rewardCountValue++;
            worldPosition = worldPositionValue;
        }
    }

    public void RecordMerge(int sourceValue, int mergedValue, Vector3 worldPositionValue, int minSourceValue, int rewardMergedValue)
    {
        RegisterMerge(sourceValue, mergedValue, worldPositionValue, minSourceValue, rewardMergedValue);
    }

    public void AddMerge(int sourceValue, int mergedValue, Vector3 worldPositionValue, int minSourceValue, int rewardMergedValue)
    {
        RegisterMerge(sourceValue, mergedValue, worldPositionValue, minSourceValue, rewardMergedValue);
    }

    public void RegisterReward(Vector3 worldPositionValue)
    {
        rewardDetected = true;
        rewardCountValue++;
        worldPosition = worldPositionValue;
    }

    public void RegisterReward(Vector3 worldPositionValue, int count)
    {
        rewardDetected = true;
        rewardCountValue += Mathf.Max(1, count);
        worldPosition = worldPositionValue;
    }
}