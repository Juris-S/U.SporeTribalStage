using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace SporeTribal.Inputs
{
    public partial class @TribalControls: IInputActionCollection2, IDisposable
    {
        public InputActionAsset asset { get; }

        public @TribalControls()
        {
            asset = InputActionAsset.FromJson(@"{
    ""version"": 1,
    ""name"": ""TribalControls"",
    ""maps"": [
        {
            ""name"": ""Camera"",
            ""id"": ""fd97adfc-1587-4bc2-82dd-7d69c1e6423a"",
            ""actions"": [
                {
                    ""name"": ""Pan"",
                    ""type"": ""Value"",
                    ""id"": ""59562c6c-5618-4c63-b057-94b1e94cd656"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true,
                    ""priority"": 0
                },
                {
                    ""name"": ""OrbitModifier"",
                    ""type"": ""Button"",
                    ""id"": ""974cb707-f6c0-4fca-9914-2e03eb9d20d0"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""ToggleAutoOrbit"",
                    ""type"": ""Button"",
                    ""id"": ""42f986a5-3587-421c-bea5-a891829b4dbb"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Arrows"",
                    ""id"": ""d8933871-bd3d-4bd8-b9a0-0aae21159e5a"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Pan"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""2c016ecf-74f3-42b8-ac6f-837224f51e99"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Pan"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""8952b271-4014-44e4-9839-cd409dac5845"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Pan"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""fa62bd3b-99b9-4b3d-8c05-34456d2bbed3"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Pan"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""2ca1d891-eb37-4431-b9b4-021de38ccd72"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Pan"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""c2abf515-a7cb-48cc-aa4d-2ef19506f4f8"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""OrbitModifier"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""588f503c-0045-482b-be40-f2bd89dc358b"",
                    ""path"": ""<Keyboard>/o"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleAutoOrbit"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Diplomacy"",
            ""id"": ""85ce7d65-9934-41d7-8e28-8b57d42484f4"",
            ""actions"": [
                {
                    ""name"": ""Horn"",
                    ""type"": ""Button"",
                    ""id"": ""d93e717e-dada-4e3e-813a-3d49b39f2bec"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""Maraca"",
                    ""type"": ""Button"",
                    ""id"": ""3f2abdaf-c3c9-4a79-947a-d56e33274ac1"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""Didgeridoo"",
                    ""type"": ""Button"",
                    ""id"": ""1cebbde1-e9ea-4ba4-872e-cd07537183f4"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""Gift"",
                    ""type"": ""Button"",
                    ""id"": ""a89c2ccf-868b-422c-94ae-3f51e1a849d3"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""LargeGiftModifier"",
                    ""type"": ""Button"",
                    ""id"": ""cfcb0f8a-5448-406c-8889-6c0ae17fbbcf"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""SoothingSong"",
                    ""type"": ""Button"",
                    ""id"": ""4351b733-62fc-4eb1-a6b8-d6f61a29e67f"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""184c9c8f-5540-4f24-80ee-a8db7900b531"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Horn"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""fb41ca89-0c66-4c7e-83f8-ca5d3421e5e8"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Maraca"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""baca9898-5afa-46ef-8f92-4003301639a0"",
                    ""path"": ""<Keyboard>/3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Didgeridoo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f792ea9b-86b3-469f-8402-1bd499f46f31"",
                    ""path"": ""<Keyboard>/g"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Gift"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""cd436699-d3bc-4630-9f60-3dffd5e8cce7"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""LargeGiftModifier"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3ab04916-bd65-44ec-be98-43a46a4156c8"",
                    ""path"": ""<Keyboard>/h"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""SoothingSong"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Interface"",
            ""id"": ""bef511ec-f9aa-4832-8c07-f1c3c6ef7a79"",
            ""actions"": [
                {
                    ""name"": ""ToggleHints"",
                    ""type"": ""Button"",
                    ""id"": ""6444539c-1a4b-4fa1-bd61-c740d6b432ec"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""99b7e908-b718-4ff6-83cb-cffac8925f06"",
                    ""path"": ""<Keyboard>/f4"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleHints"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""DevTools"",
            ""id"": ""258d2bc7-d64a-4939-bf14-7576282d9228"",
            ""actions"": [
                {
                    ""name"": ""ToggleVillageDebug"",
                    ""type"": ""Button"",
                    ""id"": ""afe4fbc7-88c2-494c-b700-8a0d471d9eab"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""RaidNow"",
                    ""type"": ""Button"",
                    ""id"": ""7516f75f-e017-4174-b525-0060a7de9461"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""StealNow"",
                    ""type"": ""Button"",
                    ""id"": ""f351064b-7f69-4b91-a5c1-434c02ef6cdf"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""CultureActivity1"",
                    ""type"": ""Button"",
                    ""id"": ""670a1e8b-b9a7-4a6e-8392-b4a57beb7a5e"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""CultureActivity2"",
                    ""type"": ""Button"",
                    ""id"": ""226409d4-6ba0-4914-83a8-1e0ac8aa2ed0"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""ShiftAggressive"",
                    ""type"": ""Button"",
                    ""id"": ""d4f869c7-e641-433b-8f0b-e8708e421056"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""ShiftFriendly"",
                    ""type"": ""Button"",
                    ""id"": ""b6ec24e8-7707-49fb-bdf1-aecbd9c99b09"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""ToggleCultureOverlay"",
                    ""type"": ""Button"",
                    ""id"": ""b6ed4a92-6f3a-4fcc-ac27-d9031bddbc38"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""CycleCultureSubject"",
                    ""type"": ""Button"",
                    ""id"": ""d36e4e99-945e-446e-acf7-cbb07a67fe77"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""HitSelected"",
                    ""type"": ""Button"",
                    ""id"": ""80c18825-b663-4f83-a9fe-204a029d4cbe"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""KillSelected"",
                    ""type"": ""Button"",
                    ""id"": ""f974da10-6ae5-479b-a5fe-dd4459b8b66e"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""ToggleWorldBend"",
                    ""type"": ""Button"",
                    ""id"": ""ccbd64e1-c411-47e9-8876-e4601e1fb776"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""ToggleNavMeshView"",
                    ""type"": ""Button"",
                    ""id"": ""3324fcdb-ac96-4f80-a7f9-c8f332e2ec34"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                },
                {
                    ""name"": ""ToggleNavMeshViewAll"",
                    ""type"": ""Button"",
                    ""id"": ""fa90faa4-01e0-40f3-8665-460ce56c637f"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false,
                    ""priority"": 0
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""f89d18f8-31a1-4fa8-8c4c-852ff8f1f84d"",
                    ""path"": ""<Keyboard>/f1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleVillageDebug"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c86260e0-1001-4aa7-bcbd-76ffee5ed3b8"",
                    ""path"": ""<Keyboard>/f2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""RaidNow"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4c5f3067-7957-4433-9f75-93b8f86bee52"",
                    ""path"": ""<Keyboard>/f3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""StealNow"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""11f8eaca-da28-4013-88f4-2dadd33eb371"",
                    ""path"": ""<Keyboard>/f5"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""CultureActivity1"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""cb4e9262-e188-426e-8b1b-de76f062e2ca"",
                    ""path"": ""<Keyboard>/f6"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""CultureActivity2"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""be9dbb68-1f2a-4e20-bda8-371ee2108939"",
                    ""path"": ""<Keyboard>/f7"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ShiftAggressive"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""927c6cef-aca3-4d75-ae92-caf3ecca3c15"",
                    ""path"": ""<Keyboard>/f8"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ShiftFriendly"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""7ba4fe02-ece2-4a48-a9d3-a602fcd56302"",
                    ""path"": ""<Keyboard>/f9"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleCultureOverlay"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ab355904-1e82-44e2-b45c-1b64a9c0f530"",
                    ""path"": ""<Keyboard>/tab"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""CycleCultureSubject"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d8734934-518a-4445-96f2-cc19944519a8"",
                    ""path"": ""<Keyboard>/insert"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""HitSelected"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""11095aeb-07d1-4594-8786-12d82a2ff90a"",
                    ""path"": ""<Keyboard>/delete"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""KillSelected"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d6eb698f-7b59-499e-b3c4-e735f2e0842e"",
                    ""path"": ""<Keyboard>/b"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleWorldBend"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2258d05a-3f07-4f31-9e95-f8674a4b0c70"",
                    ""path"": ""<Keyboard>/n"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleNavMeshView"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a50dd7dc-2f00-4aad-8dc1-2699d9bcd4c5"",
                    ""path"": ""<Keyboard>/m"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleNavMeshViewAll"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Keyboard&Mouse"",
            ""bindingGroup"": ""Keyboard&Mouse"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
            m_Camera = asset.FindActionMap("Camera", throwIfNotFound: true);
            m_Camera_Pan = m_Camera.FindAction("Pan", throwIfNotFound: true);
            m_Camera_OrbitModifier = m_Camera.FindAction("OrbitModifier", throwIfNotFound: true);
            m_Camera_ToggleAutoOrbit = m_Camera.FindAction("ToggleAutoOrbit", throwIfNotFound: true);
            m_Diplomacy = asset.FindActionMap("Diplomacy", throwIfNotFound: true);
            m_Diplomacy_Horn = m_Diplomacy.FindAction("Horn", throwIfNotFound: true);
            m_Diplomacy_Maraca = m_Diplomacy.FindAction("Maraca", throwIfNotFound: true);
            m_Diplomacy_Didgeridoo = m_Diplomacy.FindAction("Didgeridoo", throwIfNotFound: true);
            m_Diplomacy_Gift = m_Diplomacy.FindAction("Gift", throwIfNotFound: true);
            m_Diplomacy_LargeGiftModifier = m_Diplomacy.FindAction("LargeGiftModifier", throwIfNotFound: true);
            m_Diplomacy_SoothingSong = m_Diplomacy.FindAction("SoothingSong", throwIfNotFound: true);
            m_Interface = asset.FindActionMap("Interface", throwIfNotFound: true);
            m_Interface_ToggleHints = m_Interface.FindAction("ToggleHints", throwIfNotFound: true);
            m_DevTools = asset.FindActionMap("DevTools", throwIfNotFound: true);
            m_DevTools_ToggleVillageDebug = m_DevTools.FindAction("ToggleVillageDebug", throwIfNotFound: true);
            m_DevTools_RaidNow = m_DevTools.FindAction("RaidNow", throwIfNotFound: true);
            m_DevTools_StealNow = m_DevTools.FindAction("StealNow", throwIfNotFound: true);
            m_DevTools_CultureActivity1 = m_DevTools.FindAction("CultureActivity1", throwIfNotFound: true);
            m_DevTools_CultureActivity2 = m_DevTools.FindAction("CultureActivity2", throwIfNotFound: true);
            m_DevTools_ShiftAggressive = m_DevTools.FindAction("ShiftAggressive", throwIfNotFound: true);
            m_DevTools_ShiftFriendly = m_DevTools.FindAction("ShiftFriendly", throwIfNotFound: true);
            m_DevTools_ToggleCultureOverlay = m_DevTools.FindAction("ToggleCultureOverlay", throwIfNotFound: true);
            m_DevTools_CycleCultureSubject = m_DevTools.FindAction("CycleCultureSubject", throwIfNotFound: true);
            m_DevTools_HitSelected = m_DevTools.FindAction("HitSelected", throwIfNotFound: true);
            m_DevTools_KillSelected = m_DevTools.FindAction("KillSelected", throwIfNotFound: true);
            m_DevTools_ToggleWorldBend = m_DevTools.FindAction("ToggleWorldBend", throwIfNotFound: true);
            m_DevTools_ToggleNavMeshView = m_DevTools.FindAction("ToggleNavMeshView", throwIfNotFound: true);
            m_DevTools_ToggleNavMeshViewAll = m_DevTools.FindAction("ToggleNavMeshViewAll", throwIfNotFound: true);
        }

        ~@TribalControls()
        {
            UnityEngine.Debug.Assert(!m_Camera.enabled, "This will cause a leak and performance issues, TribalControls.Camera.Disable() has not been called.");
            UnityEngine.Debug.Assert(!m_Diplomacy.enabled, "This will cause a leak and performance issues, TribalControls.Diplomacy.Disable() has not been called.");
            UnityEngine.Debug.Assert(!m_Interface.enabled, "This will cause a leak and performance issues, TribalControls.Interface.Disable() has not been called.");
            UnityEngine.Debug.Assert(!m_DevTools.enabled, "This will cause a leak and performance issues, TribalControls.DevTools.Disable() has not been called.");
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(asset);
        }

        public InputBinding? bindingMask
        {
            get => asset.bindingMask;
            set => asset.bindingMask = value;
        }

        public ReadOnlyArray<InputDevice>? devices
        {
            get => asset.devices;
            set => asset.devices = value;
        }

        public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

        public bool Contains(InputAction action)
        {
            return asset.Contains(action);
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            return asset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Enable()
        {
            asset.Enable();
        }

        public void Disable()
        {
            asset.Disable();
        }

        public IEnumerable<InputBinding> bindings => asset.bindings;

        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        {
            return asset.FindAction(actionNameOrId, throwIfNotFound);
        }

        public int FindBinding(InputBinding bindingMask, out InputAction action)
        {
            return asset.FindBinding(bindingMask, out action);
        }

        private readonly InputActionMap m_Camera;
        private List<ICameraActions> m_CameraActionsCallbackInterfaces = new List<ICameraActions>();
        private readonly InputAction m_Camera_Pan;
        private readonly InputAction m_Camera_OrbitModifier;
        private readonly InputAction m_Camera_ToggleAutoOrbit;
        public struct CameraActions
        {
            private @TribalControls m_Wrapper;

            public CameraActions(@TribalControls wrapper) { m_Wrapper = wrapper; }
            public InputAction @Pan => m_Wrapper.m_Camera_Pan;
            public InputAction @OrbitModifier => m_Wrapper.m_Camera_OrbitModifier;
            public InputAction @ToggleAutoOrbit => m_Wrapper.m_Camera_ToggleAutoOrbit;
            public InputActionMap Get() { return m_Wrapper.m_Camera; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(CameraActions set) { return set.Get(); }
            public void AddCallbacks(ICameraActions instance)
            {
                if (instance == null || m_Wrapper.m_CameraActionsCallbackInterfaces.Contains(instance)) return;
                m_Wrapper.m_CameraActionsCallbackInterfaces.Add(instance);
                @Pan.started += instance.OnPan;
                @Pan.performed += instance.OnPan;
                @Pan.canceled += instance.OnPan;
                @OrbitModifier.started += instance.OnOrbitModifier;
                @OrbitModifier.performed += instance.OnOrbitModifier;
                @OrbitModifier.canceled += instance.OnOrbitModifier;
                @ToggleAutoOrbit.started += instance.OnToggleAutoOrbit;
                @ToggleAutoOrbit.performed += instance.OnToggleAutoOrbit;
                @ToggleAutoOrbit.canceled += instance.OnToggleAutoOrbit;
            }

            private void UnregisterCallbacks(ICameraActions instance)
            {
                @Pan.started -= instance.OnPan;
                @Pan.performed -= instance.OnPan;
                @Pan.canceled -= instance.OnPan;
                @OrbitModifier.started -= instance.OnOrbitModifier;
                @OrbitModifier.performed -= instance.OnOrbitModifier;
                @OrbitModifier.canceled -= instance.OnOrbitModifier;
                @ToggleAutoOrbit.started -= instance.OnToggleAutoOrbit;
                @ToggleAutoOrbit.performed -= instance.OnToggleAutoOrbit;
                @ToggleAutoOrbit.canceled -= instance.OnToggleAutoOrbit;
            }

            public void RemoveCallbacks(ICameraActions instance)
            {
                if (m_Wrapper.m_CameraActionsCallbackInterfaces.Remove(instance))
                    UnregisterCallbacks(instance);
            }

            public void SetCallbacks(ICameraActions instance)
            {
                foreach (var item in m_Wrapper.m_CameraActionsCallbackInterfaces)
                    UnregisterCallbacks(item);
                m_Wrapper.m_CameraActionsCallbackInterfaces.Clear();
                AddCallbacks(instance);
            }
        }
        public CameraActions @Camera => new CameraActions(this);

        private readonly InputActionMap m_Diplomacy;
        private List<IDiplomacyActions> m_DiplomacyActionsCallbackInterfaces = new List<IDiplomacyActions>();
        private readonly InputAction m_Diplomacy_Horn;
        private readonly InputAction m_Diplomacy_Maraca;
        private readonly InputAction m_Diplomacy_Didgeridoo;
        private readonly InputAction m_Diplomacy_Gift;
        private readonly InputAction m_Diplomacy_LargeGiftModifier;
        private readonly InputAction m_Diplomacy_SoothingSong;
        public struct DiplomacyActions
        {
            private @TribalControls m_Wrapper;

            public DiplomacyActions(@TribalControls wrapper) { m_Wrapper = wrapper; }
            public InputAction @Horn => m_Wrapper.m_Diplomacy_Horn;
            public InputAction @Maraca => m_Wrapper.m_Diplomacy_Maraca;
            public InputAction @Didgeridoo => m_Wrapper.m_Diplomacy_Didgeridoo;
            public InputAction @Gift => m_Wrapper.m_Diplomacy_Gift;
            public InputAction @LargeGiftModifier => m_Wrapper.m_Diplomacy_LargeGiftModifier;
            public InputAction @SoothingSong => m_Wrapper.m_Diplomacy_SoothingSong;
            public InputActionMap Get() { return m_Wrapper.m_Diplomacy; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(DiplomacyActions set) { return set.Get(); }
            public void AddCallbacks(IDiplomacyActions instance)
            {
                if (instance == null || m_Wrapper.m_DiplomacyActionsCallbackInterfaces.Contains(instance)) return;
                m_Wrapper.m_DiplomacyActionsCallbackInterfaces.Add(instance);
                @Horn.started += instance.OnHorn;
                @Horn.performed += instance.OnHorn;
                @Horn.canceled += instance.OnHorn;
                @Maraca.started += instance.OnMaraca;
                @Maraca.performed += instance.OnMaraca;
                @Maraca.canceled += instance.OnMaraca;
                @Didgeridoo.started += instance.OnDidgeridoo;
                @Didgeridoo.performed += instance.OnDidgeridoo;
                @Didgeridoo.canceled += instance.OnDidgeridoo;
                @Gift.started += instance.OnGift;
                @Gift.performed += instance.OnGift;
                @Gift.canceled += instance.OnGift;
                @LargeGiftModifier.started += instance.OnLargeGiftModifier;
                @LargeGiftModifier.performed += instance.OnLargeGiftModifier;
                @LargeGiftModifier.canceled += instance.OnLargeGiftModifier;
                @SoothingSong.started += instance.OnSoothingSong;
                @SoothingSong.performed += instance.OnSoothingSong;
                @SoothingSong.canceled += instance.OnSoothingSong;
            }

            private void UnregisterCallbacks(IDiplomacyActions instance)
            {
                @Horn.started -= instance.OnHorn;
                @Horn.performed -= instance.OnHorn;
                @Horn.canceled -= instance.OnHorn;
                @Maraca.started -= instance.OnMaraca;
                @Maraca.performed -= instance.OnMaraca;
                @Maraca.canceled -= instance.OnMaraca;
                @Didgeridoo.started -= instance.OnDidgeridoo;
                @Didgeridoo.performed -= instance.OnDidgeridoo;
                @Didgeridoo.canceled -= instance.OnDidgeridoo;
                @Gift.started -= instance.OnGift;
                @Gift.performed -= instance.OnGift;
                @Gift.canceled -= instance.OnGift;
                @LargeGiftModifier.started -= instance.OnLargeGiftModifier;
                @LargeGiftModifier.performed -= instance.OnLargeGiftModifier;
                @LargeGiftModifier.canceled -= instance.OnLargeGiftModifier;
                @SoothingSong.started -= instance.OnSoothingSong;
                @SoothingSong.performed -= instance.OnSoothingSong;
                @SoothingSong.canceled -= instance.OnSoothingSong;
            }

            public void RemoveCallbacks(IDiplomacyActions instance)
            {
                if (m_Wrapper.m_DiplomacyActionsCallbackInterfaces.Remove(instance))
                    UnregisterCallbacks(instance);
            }

            public void SetCallbacks(IDiplomacyActions instance)
            {
                foreach (var item in m_Wrapper.m_DiplomacyActionsCallbackInterfaces)
                    UnregisterCallbacks(item);
                m_Wrapper.m_DiplomacyActionsCallbackInterfaces.Clear();
                AddCallbacks(instance);
            }
        }
        public DiplomacyActions @Diplomacy => new DiplomacyActions(this);

        private readonly InputActionMap m_Interface;
        private List<IInterfaceActions> m_InterfaceActionsCallbackInterfaces = new List<IInterfaceActions>();
        private readonly InputAction m_Interface_ToggleHints;
        public struct InterfaceActions
        {
            private @TribalControls m_Wrapper;

            public InterfaceActions(@TribalControls wrapper) { m_Wrapper = wrapper; }
            public InputAction @ToggleHints => m_Wrapper.m_Interface_ToggleHints;
            public InputActionMap Get() { return m_Wrapper.m_Interface; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(InterfaceActions set) { return set.Get(); }
            public void AddCallbacks(IInterfaceActions instance)
            {
                if (instance == null || m_Wrapper.m_InterfaceActionsCallbackInterfaces.Contains(instance)) return;
                m_Wrapper.m_InterfaceActionsCallbackInterfaces.Add(instance);
                @ToggleHints.started += instance.OnToggleHints;
                @ToggleHints.performed += instance.OnToggleHints;
                @ToggleHints.canceled += instance.OnToggleHints;
            }

            private void UnregisterCallbacks(IInterfaceActions instance)
            {
                @ToggleHints.started -= instance.OnToggleHints;
                @ToggleHints.performed -= instance.OnToggleHints;
                @ToggleHints.canceled -= instance.OnToggleHints;
            }

            public void RemoveCallbacks(IInterfaceActions instance)
            {
                if (m_Wrapper.m_InterfaceActionsCallbackInterfaces.Remove(instance))
                    UnregisterCallbacks(instance);
            }

            public void SetCallbacks(IInterfaceActions instance)
            {
                foreach (var item in m_Wrapper.m_InterfaceActionsCallbackInterfaces)
                    UnregisterCallbacks(item);
                m_Wrapper.m_InterfaceActionsCallbackInterfaces.Clear();
                AddCallbacks(instance);
            }
        }
        public InterfaceActions @Interface => new InterfaceActions(this);

        private readonly InputActionMap m_DevTools;
        private List<IDevToolsActions> m_DevToolsActionsCallbackInterfaces = new List<IDevToolsActions>();
        private readonly InputAction m_DevTools_ToggleVillageDebug;
        private readonly InputAction m_DevTools_RaidNow;
        private readonly InputAction m_DevTools_StealNow;
        private readonly InputAction m_DevTools_CultureActivity1;
        private readonly InputAction m_DevTools_CultureActivity2;
        private readonly InputAction m_DevTools_ShiftAggressive;
        private readonly InputAction m_DevTools_ShiftFriendly;
        private readonly InputAction m_DevTools_ToggleCultureOverlay;
        private readonly InputAction m_DevTools_CycleCultureSubject;
        private readonly InputAction m_DevTools_HitSelected;
        private readonly InputAction m_DevTools_KillSelected;
        private readonly InputAction m_DevTools_ToggleWorldBend;
        private readonly InputAction m_DevTools_ToggleNavMeshView;
        private readonly InputAction m_DevTools_ToggleNavMeshViewAll;
        public struct DevToolsActions
        {
            private @TribalControls m_Wrapper;

            public DevToolsActions(@TribalControls wrapper) { m_Wrapper = wrapper; }
            public InputAction @ToggleVillageDebug => m_Wrapper.m_DevTools_ToggleVillageDebug;
            public InputAction @RaidNow => m_Wrapper.m_DevTools_RaidNow;
            public InputAction @StealNow => m_Wrapper.m_DevTools_StealNow;
            public InputAction @CultureActivity1 => m_Wrapper.m_DevTools_CultureActivity1;
            public InputAction @CultureActivity2 => m_Wrapper.m_DevTools_CultureActivity2;
            public InputAction @ShiftAggressive => m_Wrapper.m_DevTools_ShiftAggressive;
            public InputAction @ShiftFriendly => m_Wrapper.m_DevTools_ShiftFriendly;
            public InputAction @ToggleCultureOverlay => m_Wrapper.m_DevTools_ToggleCultureOverlay;
            public InputAction @CycleCultureSubject => m_Wrapper.m_DevTools_CycleCultureSubject;
            public InputAction @HitSelected => m_Wrapper.m_DevTools_HitSelected;
            public InputAction @KillSelected => m_Wrapper.m_DevTools_KillSelected;
            public InputAction @ToggleWorldBend => m_Wrapper.m_DevTools_ToggleWorldBend;
            public InputAction @ToggleNavMeshView => m_Wrapper.m_DevTools_ToggleNavMeshView;
            public InputAction @ToggleNavMeshViewAll => m_Wrapper.m_DevTools_ToggleNavMeshViewAll;
            public InputActionMap Get() { return m_Wrapper.m_DevTools; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(DevToolsActions set) { return set.Get(); }
            public void AddCallbacks(IDevToolsActions instance)
            {
                if (instance == null || m_Wrapper.m_DevToolsActionsCallbackInterfaces.Contains(instance)) return;
                m_Wrapper.m_DevToolsActionsCallbackInterfaces.Add(instance);
                @ToggleVillageDebug.started += instance.OnToggleVillageDebug;
                @ToggleVillageDebug.performed += instance.OnToggleVillageDebug;
                @ToggleVillageDebug.canceled += instance.OnToggleVillageDebug;
                @RaidNow.started += instance.OnRaidNow;
                @RaidNow.performed += instance.OnRaidNow;
                @RaidNow.canceled += instance.OnRaidNow;
                @StealNow.started += instance.OnStealNow;
                @StealNow.performed += instance.OnStealNow;
                @StealNow.canceled += instance.OnStealNow;
                @CultureActivity1.started += instance.OnCultureActivity1;
                @CultureActivity1.performed += instance.OnCultureActivity1;
                @CultureActivity1.canceled += instance.OnCultureActivity1;
                @CultureActivity2.started += instance.OnCultureActivity2;
                @CultureActivity2.performed += instance.OnCultureActivity2;
                @CultureActivity2.canceled += instance.OnCultureActivity2;
                @ShiftAggressive.started += instance.OnShiftAggressive;
                @ShiftAggressive.performed += instance.OnShiftAggressive;
                @ShiftAggressive.canceled += instance.OnShiftAggressive;
                @ShiftFriendly.started += instance.OnShiftFriendly;
                @ShiftFriendly.performed += instance.OnShiftFriendly;
                @ShiftFriendly.canceled += instance.OnShiftFriendly;
                @ToggleCultureOverlay.started += instance.OnToggleCultureOverlay;
                @ToggleCultureOverlay.performed += instance.OnToggleCultureOverlay;
                @ToggleCultureOverlay.canceled += instance.OnToggleCultureOverlay;
                @CycleCultureSubject.started += instance.OnCycleCultureSubject;
                @CycleCultureSubject.performed += instance.OnCycleCultureSubject;
                @CycleCultureSubject.canceled += instance.OnCycleCultureSubject;
                @HitSelected.started += instance.OnHitSelected;
                @HitSelected.performed += instance.OnHitSelected;
                @HitSelected.canceled += instance.OnHitSelected;
                @KillSelected.started += instance.OnKillSelected;
                @KillSelected.performed += instance.OnKillSelected;
                @KillSelected.canceled += instance.OnKillSelected;
                @ToggleWorldBend.started += instance.OnToggleWorldBend;
                @ToggleWorldBend.performed += instance.OnToggleWorldBend;
                @ToggleWorldBend.canceled += instance.OnToggleWorldBend;
                @ToggleNavMeshView.started += instance.OnToggleNavMeshView;
                @ToggleNavMeshView.performed += instance.OnToggleNavMeshView;
                @ToggleNavMeshView.canceled += instance.OnToggleNavMeshView;
                @ToggleNavMeshViewAll.started += instance.OnToggleNavMeshViewAll;
                @ToggleNavMeshViewAll.performed += instance.OnToggleNavMeshViewAll;
                @ToggleNavMeshViewAll.canceled += instance.OnToggleNavMeshViewAll;
            }

            private void UnregisterCallbacks(IDevToolsActions instance)
            {
                @ToggleVillageDebug.started -= instance.OnToggleVillageDebug;
                @ToggleVillageDebug.performed -= instance.OnToggleVillageDebug;
                @ToggleVillageDebug.canceled -= instance.OnToggleVillageDebug;
                @RaidNow.started -= instance.OnRaidNow;
                @RaidNow.performed -= instance.OnRaidNow;
                @RaidNow.canceled -= instance.OnRaidNow;
                @StealNow.started -= instance.OnStealNow;
                @StealNow.performed -= instance.OnStealNow;
                @StealNow.canceled -= instance.OnStealNow;
                @CultureActivity1.started -= instance.OnCultureActivity1;
                @CultureActivity1.performed -= instance.OnCultureActivity1;
                @CultureActivity1.canceled -= instance.OnCultureActivity1;
                @CultureActivity2.started -= instance.OnCultureActivity2;
                @CultureActivity2.performed -= instance.OnCultureActivity2;
                @CultureActivity2.canceled -= instance.OnCultureActivity2;
                @ShiftAggressive.started -= instance.OnShiftAggressive;
                @ShiftAggressive.performed -= instance.OnShiftAggressive;
                @ShiftAggressive.canceled -= instance.OnShiftAggressive;
                @ShiftFriendly.started -= instance.OnShiftFriendly;
                @ShiftFriendly.performed -= instance.OnShiftFriendly;
                @ShiftFriendly.canceled -= instance.OnShiftFriendly;
                @ToggleCultureOverlay.started -= instance.OnToggleCultureOverlay;
                @ToggleCultureOverlay.performed -= instance.OnToggleCultureOverlay;
                @ToggleCultureOverlay.canceled -= instance.OnToggleCultureOverlay;
                @CycleCultureSubject.started -= instance.OnCycleCultureSubject;
                @CycleCultureSubject.performed -= instance.OnCycleCultureSubject;
                @CycleCultureSubject.canceled -= instance.OnCycleCultureSubject;
                @HitSelected.started -= instance.OnHitSelected;
                @HitSelected.performed -= instance.OnHitSelected;
                @HitSelected.canceled -= instance.OnHitSelected;
                @KillSelected.started -= instance.OnKillSelected;
                @KillSelected.performed -= instance.OnKillSelected;
                @KillSelected.canceled -= instance.OnKillSelected;
                @ToggleWorldBend.started -= instance.OnToggleWorldBend;
                @ToggleWorldBend.performed -= instance.OnToggleWorldBend;
                @ToggleWorldBend.canceled -= instance.OnToggleWorldBend;
                @ToggleNavMeshView.started -= instance.OnToggleNavMeshView;
                @ToggleNavMeshView.performed -= instance.OnToggleNavMeshView;
                @ToggleNavMeshView.canceled -= instance.OnToggleNavMeshView;
                @ToggleNavMeshViewAll.started -= instance.OnToggleNavMeshViewAll;
                @ToggleNavMeshViewAll.performed -= instance.OnToggleNavMeshViewAll;
                @ToggleNavMeshViewAll.canceled -= instance.OnToggleNavMeshViewAll;
            }

            public void RemoveCallbacks(IDevToolsActions instance)
            {
                if (m_Wrapper.m_DevToolsActionsCallbackInterfaces.Remove(instance))
                    UnregisterCallbacks(instance);
            }

            public void SetCallbacks(IDevToolsActions instance)
            {
                foreach (var item in m_Wrapper.m_DevToolsActionsCallbackInterfaces)
                    UnregisterCallbacks(item);
                m_Wrapper.m_DevToolsActionsCallbackInterfaces.Clear();
                AddCallbacks(instance);
            }
        }
        public DevToolsActions @DevTools => new DevToolsActions(this);
        private int m_KeyboardMouseSchemeIndex = -1;
        public InputControlScheme KeyboardMouseScheme
        {
            get
            {
                if (m_KeyboardMouseSchemeIndex == -1) m_KeyboardMouseSchemeIndex = asset.FindControlSchemeIndex("Keyboard&Mouse");
                return asset.controlSchemes[m_KeyboardMouseSchemeIndex];
            }
        }
        public interface ICameraActions
        {
            void OnPan(InputAction.CallbackContext context);
            void OnOrbitModifier(InputAction.CallbackContext context);
            void OnToggleAutoOrbit(InputAction.CallbackContext context);
        }
        public interface IDiplomacyActions
        {
            void OnHorn(InputAction.CallbackContext context);
            void OnMaraca(InputAction.CallbackContext context);
            void OnDidgeridoo(InputAction.CallbackContext context);
            void OnGift(InputAction.CallbackContext context);
            void OnLargeGiftModifier(InputAction.CallbackContext context);
            void OnSoothingSong(InputAction.CallbackContext context);
        }
        public interface IInterfaceActions
        {
            void OnToggleHints(InputAction.CallbackContext context);
        }
        public interface IDevToolsActions
        {
            void OnToggleVillageDebug(InputAction.CallbackContext context);
            void OnRaidNow(InputAction.CallbackContext context);
            void OnStealNow(InputAction.CallbackContext context);
            void OnCultureActivity1(InputAction.CallbackContext context);
            void OnCultureActivity2(InputAction.CallbackContext context);
            void OnShiftAggressive(InputAction.CallbackContext context);
            void OnShiftFriendly(InputAction.CallbackContext context);
            void OnToggleCultureOverlay(InputAction.CallbackContext context);
            void OnCycleCultureSubject(InputAction.CallbackContext context);
            void OnHitSelected(InputAction.CallbackContext context);
            void OnKillSelected(InputAction.CallbackContext context);
            void OnToggleWorldBend(InputAction.CallbackContext context);
            void OnToggleNavMeshView(InputAction.CallbackContext context);
            void OnToggleNavMeshViewAll(InputAction.CallbackContext context);
        }
    }
}
