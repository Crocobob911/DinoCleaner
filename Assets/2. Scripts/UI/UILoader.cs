using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DinoCleaner.UI
{
    /// <summary>
    /// 씬마다 하나씩 배치되어, Addressables 주소(= 프리팹 이름)로 UI 프리팹을 로드하고
    /// 레이어별 Canvas 아래에 생성/표시/숨김/해제합니다.
    /// </summary>
    public class UILoader : MonoBehaviour
    {
        #region Fields & Properties
        [SerializeField] private Transform canvas_FullScreen;
        [SerializeField] private Transform canvas_Popup;
        [SerializeField] private Transform canvas_Top;

        [Tooltip("씬 시작 시 자동으로 Show할 UI 주소 목록")]
        [SerializeField] private List<string> awakeUIList = new List<string>();

        // 프리팹 에셋 로드 핸들 (Release 대상)
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> loadUIHandles = new();
        // 생성된 UI 인스턴스
        private readonly Dictionary<string, GameObject> loadedUIs = new();
        // 닫기 연출이 진행 중인 UI
        private readonly HashSet<string> closingUIs = new();

        private Transform stagingRoot;
        #endregion

        #region Singleton & initialization
        public static UILoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        #endregion

        #region public methods

        public bool IsShowing(string uiName)
        {
            return loadedUIs.TryGetValue(uiName, out GameObject inst) && inst != null
                   && inst.activeSelf && !closingUIs.Contains(uiName);
        }

        /// <summary>
        /// 데이터를 전달하지 않고 UI를 활성화합니다.
        /// </summary>
        public void ShowUI(string uiName)
        {
            LoadUI(uiName, instance =>
            {
                if (instance == null) return;
                Activate(uiName, instance);
            });
        }

        /// <summary>
        /// 단일 데이터 또는 ValueTuple로 묶인 다중 데이터를 전달하며 UI를 활성화합니다.
        /// </summary>
        public void ShowUI<T>(string uiName, T data)
        {
            LoadUI(uiName, instance =>
            {
                if (instance == null) return;
                instance.SetActive(true);
                SendDataToUI(instance, data);
                Activate(uiName, instance);
            });
        }

        public void HideUI(string uiName)
        {
            if (!loadedUIs.TryGetValue(uiName, out GameObject uiInstance))
            {
                Debug.LogWarning($"[UILoader] {uiName} 는 로드된 상태가 아닙니다.");
                return;
            }

            if (uiInstance == null)
            {
                Debug.LogWarning($"[UILoader] {uiName} 인스턴스가 null 입니다. 언로드합니다.");
                UnloadUI(uiName);
                return;
            }

            // 이미 숨겨졌거나 닫는 중이면 무시 (CloseAction 내부에서 HideUI를 다시 불러도 안전)
            if (!uiInstance.activeSelf || closingUIs.Contains(uiName)) return;

            if (uiInstance.TryGetComponent<UI_Popup>(out var popup))
            {
                closingUIs.Add(uiName);
                popup.CloseAction(() =>
                {
                    // 닫는 도중 다시 Show된 경우 closingUIs에서 빠져 있으므로 끄지 않음
                    if (!closingUIs.Remove(uiName)) return;
                    if (uiInstance != null) uiInstance.SetActive(false);
                });
            }
            else
            {
                uiInstance.SetActive(false);
            }
        }

        /// <summary>
        /// Addressables로 UI 프리팹을 비동기 로드하고 인스턴스화합니다. (생성 직후에는 비활성 상태)
        /// </summary>
        public void LoadUI(string uiName, Action<GameObject> onInstanceCreated = null)
        {
            // 1. 인스턴스가 이미 존재
            if (loadedUIs.TryGetValue(uiName, out GameObject existingInstance))
            {
                if (existingInstance != null)
                {
                    onInstanceCreated?.Invoke(existingInstance);
                    return;
                }
                loadedUIs.Remove(uiName); // 외부에서 Destroy된 경우
            }

            // 2. 프리팹 핸들이 이미 존재 (로딩 중이거나 로드 완료)
            if (loadUIHandles.TryGetValue(uiName, out var existingHandle))
            {
                if (existingHandle.IsDone)
                {
                    onInstanceCreated?.Invoke(GetOrCreateInstance(uiName, existingHandle.Result));
                }
                else
                {
                    existingHandle.Completed += handle =>
                    {
                        onInstanceCreated?.Invoke(handle.Status == AsyncOperationStatus.Succeeded
                            ? GetOrCreateInstance(uiName, handle.Result)
                            : null);
                    };
                }
                return;
            }

            // 3. 새로 로드
            AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(uiName);
            loadUIHandles.Add(uiName, loadHandle);

            loadHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    onInstanceCreated?.Invoke(GetOrCreateInstance(uiName, handle.Result));
                }
                else
                {
                    Debug.LogError($"[UILoader] UI 로드 실패: {uiName}");
                    loadUIHandles.Remove(uiName);
                    Addressables.Release(handle);
                    onInstanceCreated?.Invoke(null);
                }
            };
        }

        /// <summary>
        /// 인스턴스를 파괴하고 프리팹 에셋을 Release합니다.
        /// </summary>
        public void UnloadUI(string uiName)
        {
            closingUIs.Remove(uiName);

            if (loadedUIs.TryGetValue(uiName, out GameObject uiInstance))
            {
                if (uiInstance != null) Destroy(uiInstance);
                loadedUIs.Remove(uiName);
            }

            if (loadUIHandles.TryGetValue(uiName, out var loadHandle))
            {
                Addressables.Release(loadHandle);
                loadUIHandles.Remove(uiName);
            }
        }

        #endregion

        #region private methods

        private void Activate(string uiName, GameObject instance)
        {
            closingUIs.Remove(uiName);
            instance.SetActive(true);
            instance.transform.SetAsLastSibling();
            if (instance.TryGetComponent<UI_Popup>(out var popup))
            {
                popup.OpenAction();
            }
        }

        private GameObject GetOrCreateInstance(string uiName, GameObject uiPrefab)
        {
            // 같은 프레임에 여러 요청이 몰려도 인스턴스는 하나만 생성
            if (loadedUIs.TryGetValue(uiName, out GameObject inst) && inst != null) return inst;

            Transform targetParent = canvas_FullScreen;
            if (uiPrefab.TryGetComponent<UI_ILayerInfo>(out var layerInfo))
            {
                targetParent = layerInfo.TargetLayer switch
                {
                    EUILayer.Popup => canvas_Popup,
                    EUILayer.Top => canvas_Top,
                    _ => canvas_FullScreen
                };
            }

            // 비활성 부모 아래에서 생성 후 꺼서 옮김 → 생성 즉시 Awake/OnEnable이 돌지 않음
            // (에디터에선 handle.Result가 프리팹 에셋 원본이라 프리팹 자체를 건드리지 않음)
            GameObject uiInstance = Instantiate(uiPrefab, GetStagingRoot());
            uiInstance.SetActive(false);
            uiInstance.transform.SetParent(targetParent, false);

            uiInstance.name = uiName;
            loadedUIs[uiName] = uiInstance;
            return uiInstance;
        }

        private Transform GetStagingRoot()
        {
            if (stagingRoot == null)
            {
                var go = new GameObject("_UIStaging");
                go.SetActive(false);
                go.transform.SetParent(transform, false);
                stagingRoot = go.transform;
            }
            return stagingRoot;
        }

        private void SendDataToUI<T>(GameObject uiInstance, T data)
        {
            if (data == null) return;

            if (uiInstance.TryGetComponent<UI_IDataReceiver<T>>(out var receiver))
            {
                receiver.ReceiveData(data);
            }
            else
            {
                Debug.LogWarning($"[UILoader] {uiInstance.name} 에 UI_IDataReceiver<{typeof(T).Name}> 가 없습니다.");
            }
        }

        private void ShowUIOnSceneStart()
        {
            foreach (var uiName in awakeUIList)
            {
                if (!string.IsNullOrEmpty(uiName)) ShowUI(uiName);
            }
        }

        #endregion

        #region Unity event methods

        private void Start()
        {
            ShowUIOnSceneStart();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            foreach (var handle in loadUIHandles.Values)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
            loadUIHandles.Clear();
            loadedUIs.Clear();
            closingUIs.Clear();
            Instance = null;
        }

        #endregion
    }
}
