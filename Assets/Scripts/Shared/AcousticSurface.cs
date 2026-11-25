using UnityEngine;

namespace TheCopy.Shared
{
    /// <summary>
    /// Типи фізичних матеріалів стін і приміщень для реалістичної акустичної моделі VoIP та слуху NPC.
    /// Використовується у VoiceNoiseEmitter (фільтрація частот голосу) та сенсорах ШІ.
    /// </summary>
    public enum AcousticMaterialType : byte
    {
        Concrete = 0,     // Армований бетон бункера (сильне глушіння, холодний гул)
        Wood = 1,         // Дерев'яні панелі та меблі (теплий звук, помірна провідність)
        Glass = 2,        // Броньоване/оглядове скло (візуальний контакт, глухий голос)
        Metal = 3,        // Сталеві перегородки, гермошлюзи, вентиляція (металевий резонанс)
        AcousticFoam = 4, // Акустичний поролон безлунної камери (майже повне поглинання)
        Tile = 5,         // Кахельна плитка медблоку та санпропускника (дзвінке відлуння)
        Brick = 6         // Стара цегла (середньо-високе поглинання)
    }

    /// <summary>
    /// Компонент акустичних властивостей стіни, перегородки чи вікна.
    /// При Raycast/Linecast між гравцями VoiceNoiseEmitter зчитує цей компонент
    /// і підлаштовує частоту зрізу AudioLowPassFilter та загасання гучності.
    /// </summary>
    public class AcousticSurface : MonoBehaviour
    {
        [Header("Акустичний матеріал поверхні")]
        public AcousticMaterialType materialType = AcousticMaterialType.Concrete;

        [Tooltip("Частота зрізу LowPass-фільтра (Гц), коли звук проходить крізь цю перешкоду")]
        [Range(250f, 5000f)]
        public float lowPassCutoffHz = 680f;

        [Tooltip("Коефіцієнт збереження гучності при проходженні крізь 1 шар матеріалу (0..1)")]
        [Range(0.05f, 0.95f)]
        public float volumeTransmission = 0.35f;

        [Tooltip("Коефіцієнт відбиття звуку всередині кімнати (для розрахунку реверберації)")]
        [Range(0.0f, 1.0f)]
        public float reflectivity = 0.72f;

        /// <summary>
        /// Застосовує фізично обґрунтовані акустичні параметри для обраного матеріалу.
        /// </summary>
        public void ApplyPreset(AcousticMaterialType type)
        {
            materialType = type;
            switch (type)
            {
                case AcousticMaterialType.Concrete:
                    lowPassCutoffHz = 650f;
                    volumeTransmission = 0.30f;
                    reflectivity = 0.82f;
                    break;

                case AcousticMaterialType.Wood:
                    lowPassCutoffHz = 1650f;
                    volumeTransmission = 0.62f;
                    reflectivity = 0.38f;
                    break;

                case AcousticMaterialType.Glass:
                    lowPassCutoffHz = 1150f;
                    volumeTransmission = 0.48f;
                    reflectivity = 0.88f;
                    break;

                case AcousticMaterialType.Metal:
                    lowPassCutoffHz = 950f;
                    volumeTransmission = 0.42f;
                    reflectivity = 0.92f;
                    break;

                case AcousticMaterialType.AcousticFoam:
                    lowPassCutoffHz = 420f;
                    volumeTransmission = 0.14f;
                    reflectivity = 0.05f;
                    break;

                case AcousticMaterialType.Tile:
                    lowPassCutoffHz = 1350f;
                    volumeTransmission = 0.50f;
                    reflectivity = 0.95f;
                    break;

                case AcousticMaterialType.Brick:
                    lowPassCutoffHz = 780f;
                    volumeTransmission = 0.36f;
                    reflectivity = 0.65f;
                    break;
            }
        }
    }

    /// <summary>
    /// Акустична зона приміщення (додає відповідний пресет AudioReverbZone для курсової/диплома).
    /// </summary>
    public class AcousticRoomZone : MonoBehaviour
    {
        public string roomName = "Сектор комплексу";
        public AcousticMaterialType dominantMaterial = AcousticMaterialType.Concrete;
        public AudioReverbPreset reverbPreset = AudioReverbPreset.StoneCorridor;
        public float roomRadius = 12f;
    }
}