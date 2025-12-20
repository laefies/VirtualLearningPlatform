using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using DG.Tweening;

[RequireComponent(typeof(SharedGrabbable))]
[RequireComponent(typeof(LineRenderer))]
public class SolarEnergySystem : NetworkBehaviour
{
    [Header("Solar Panel Configuration")]
    [SerializeField] private Transform solarPanelTransform;

    [Header("Beam Visual Settings")]
    [SerializeField] private float beamStartWidth = 0.07f;
    [SerializeField] private float beamEndWidth = 0.1f;

    [Header("UI References")]
    [SerializeField] private Text angleText;
    [SerializeField] private Text powerText;
    [SerializeField] private Transform powerPointer;
    [SerializeField] private Text lightText;
    [SerializeField] private Slider lightSlider;
    [SerializeField] private UnityEngine.UI.Toggle dockToggle;

    [Header("Cloud Settings")]
    [SerializeField] private Transform cloudParent;
    [SerializeField] private float cloudRiseDistance = 1.5f;
    [SerializeField] private float cloudAppearDuration = 0.5f;
    [SerializeField] private float cloudDisappearDuration = 0.2f;


    // Network state
    private NetworkVariable<float> _incidenceAngle = new NetworkVariable<float>(
        90f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> _lightIntensity = new NetworkVariable<float>(
        1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> _powerOutput = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private SharedGrabbable _grabbable;
    private LineRenderer _beam;

    private static readonly float PanelEfficiency = 0.18f;
    private static readonly float PanelArea       = 2f;
    private static readonly float MinPowerOutput  = 0f;
    private static readonly float MaxPowerOutput  = 800f * PanelArea * PanelEfficiency;

    private void Awake()
    {
        _grabbable = GetComponent<SharedGrabbable>();
        _beam      = GetComponent<LineRenderer>();
        
        solarPanelTransform ??= GetComponentInParent<SharedObject>()?.transform;

        SetupBeam();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _incidenceAngle.OnValueChanged += OnAngleChanged;
        _lightIntensity.OnValueChanged += OnLightChanged;
        _powerOutput.OnValueChanged    += OnPowerChanged;

        if (_grabbable != null)
            _grabbable.OnDockedChanged += OnDockedChanged;

        lightSlider?.onValueChanged.AddListener(OnLightSliderChanged);

        UpdateAngleUI(_incidenceAngle.Value);
        UpdateLightUI(_lightIntensity.Value);
        UpdatePowerUI(_powerOutput.Value);
    }

    private void SetupBeam()
    {
        if (_beam != null) {
            _beam.startWidth = beamStartWidth;
            _beam.endWidth = beamEndWidth;
        }
    }

    private void Update()
    {
        if (IsServer && solarPanelTransform != null) {
            CalculateIncidenceAngle();
            CalculatePowerOutput();
        }

        UpdateBeamVisual();
    }

    #region Physics Calculations (Server Only)

    private void CalculateIncidenceAngle()
    {
        Vector3 sunDirection = solarPanelTransform.InverseTransformDirection(
            (transform.position - solarPanelTransform.position).normalized
        );

        // 0° = directly above, 90° = horizon, >90° = below
        float newAngle = Mathf.Round(90f - Mathf.Asin(sunDirection.y) * Mathf.Rad2Deg);
        
        if (Mathf.Abs(_incidenceAngle.Value - newAngle) > 0.1f)
            _incidenceAngle.Value = newAngle;
    }

    private void CalculatePowerOutput()
    {
        // Direct Normal Irradiance with atmospheric attenuation
        float directNormalIrradiance = 800f * Mathf.Exp(-3f * _lightIntensity.Value);
        
        float incidenceFactor = Mathf.Cos(_incidenceAngle.Value * Mathf.Deg2Rad);
        float newPower = directNormalIrradiance * incidenceFactor * PanelArea * PanelEfficiency;
        
        if (Mathf.Abs(_powerOutput.Value - newPower) > 0.1f)
            _powerOutput.Value = Mathf.Max(0f, newPower);
    }

    #endregion

    #region UI Updates

    private void OnAngleChanged(float oldValue, float newValue)
    {
        UpdateAngleUI(newValue);
    }

    private void OnLightChanged(float oldValue, float newValue)
    {
        UpdateLightUI(newValue);
    }

    private void OnPowerChanged(float oldValue, float newValue)
    {
        UpdatePowerUI(newValue);
    }

    private void OnDockedChanged(bool isDocked)
    {
        if (dockToggle != null)
            dockToggle.isOn = isDocked;
    }

    private void UpdateAngleUI(float angle)
    {
        if (angleText != null)
            angleText.text = (angle <= 90) ? $"{angle:F0}°" : "-";
    }

    private void UpdatePowerUI(float power)
    {
        if (powerText != null)
            powerText.text = $"{power:F1}W";

        if (powerPointer != null)
        {
            float pointerRotation = Mathf.Lerp(120f, -120f, 
                Mathf.InverseLerp(MinPowerOutput, MaxPowerOutput, power));
            powerPointer.localRotation = Quaternion.Euler(0, 0, pointerRotation);
        }
    }

    private void UpdateLightUI(float lightIntensity)
    {
        if (lightSlider != null)
            lightSlider.SetValueWithoutNotify(lightIntensity);

        if (lightText != null)
            lightText.text = $"{Mathf.Round(lightIntensity * 100)}%";

        UpdateCloudVisibility(lightIntensity);
    }

    private void OnLightSliderChanged(float value)
    {
        SetLightIntensity(value);
    }

    #endregion

    #region Visual Effects

    private void UpdateBeamVisual()
    {
        if (_beam == null || solarPanelTransform == null) return;

        bool isAngleValid = _incidenceAngle.Value <= 90;
        _beam.enabled = isAngleValid;

        if (isAngleValid)
        {
            _beam.SetPosition(0, transform.position);
            _beam.SetPosition(1, solarPanelTransform.position);

            float lightIntensity = _lightIntensity.Value;
            _beam.endWidth = Mathf.Max(beamEndWidth * (1f - lightIntensity), beamStartWidth);

            Color beamColor = _beam.startColor;
            beamColor.a = 1f - lightIntensity;
            _beam.startColor = beamColor;
        }
    }

    private void UpdateCloudVisibility(float lightIntensity)
    {
        if (cloudParent == null) return;

        int totalClouds = cloudParent.childCount;
        int cloudsToShow = Mathf.RoundToInt(totalClouds * lightIntensity);

        for (int i = 0; i < totalClouds; i++)
        {
            GameObject cloud = cloudParent.GetChild(i).gameObject;

            if (i < cloudsToShow)
                ShowCloud(cloud);
            else
                HideCloud(cloud);
        }
    }

    private void ShowCloud(GameObject cloud)
    {
        if (cloud.activeSelf) return;

        Vector3 targetPosition = cloud.transform.localPosition;
        Vector3 startPosition = targetPosition + Vector3.down * cloudRiseDistance;

        cloud.transform.localPosition = startPosition;
        cloud.SetActive(true);
        cloud.transform.DOLocalMove(targetPosition, cloudAppearDuration)
            .SetEase(Ease.OutQuad);
    }

    private void HideCloud(GameObject cloud)
    {
        if (!cloud.activeSelf || DOTween.IsTweening(cloud.transform)) return;

        Vector3 startPosition = cloud.transform.localPosition;
        Vector3 targetPosition = startPosition + Vector3.down * cloudRiseDistance;

        cloud.transform.DOLocalMove(targetPosition, cloudDisappearDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                cloud.SetActive(false);
                cloud.transform.localPosition = startPosition;
            });
    }

    #endregion

    #region Public API

    /// <summary>
    /// Set light intensity (0 = full clouds, 1 = clear sky)
    /// </summary>
    public void SetLightIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        
        if (Mathf.Abs(_lightIntensity.Value - intensity) > 0.01f)
            UpdateLightIntensityServerRpc(intensity);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateLightIntensityServerRpc(float intensity)
    {
        _lightIntensity.Value = Mathf.Clamp01(intensity);
    }

    #endregion

    public override void OnNetworkDespawn()
    {
        _incidenceAngle.OnValueChanged -= OnAngleChanged;
        _lightIntensity.OnValueChanged -= OnLightChanged;
        _powerOutput.OnValueChanged -= OnPowerChanged;

        if (_grabbable != null)
            _grabbable.OnDockedChanged -= OnDockedChanged;

        if (lightSlider != null)
            lightSlider.onValueChanged.RemoveListener(OnLightSliderChanged);

        if (cloudParent != null) {
            for (int i = 0; i < cloudParent.childCount; i++)
                cloudParent.GetChild(i).DOKill();
        }

        base.OnNetworkDespawn();
    }
}