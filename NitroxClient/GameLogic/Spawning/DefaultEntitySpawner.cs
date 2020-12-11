using System.Collections;
using NitroxClient.GameLogic.Spawning.Metadata;
using NitroxClient.MonoBehaviours;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using NitroxModel_Subnautica.DataStructures;
using UnityEngine;
using UWE;

namespace NitroxClient.GameLogic.Spawning
{
    public class DefaultEntitySpawner : IEntitySpawner
    {
        public IEnumerator Spawn(TaskResult<Optional<GameObject>> result, Entity entity, Optional<GameObject> parent, EntityCell cellRoot)
        {
            TechType techType = entity.TechType.ToUnity();

            TaskResult<GameObject> gameObjectResult = new TaskResult<GameObject>();
            yield return CreateGameObject(gameObjectResult, techType, entity.ClassId);

            GameObject gameObject = gameObjectResult.Get();
            gameObject.transform.position = entity.Transform.Position.ToUnity();
            gameObject.transform.rotation = entity.Transform.Rotation.ToUnity();
            gameObject.transform.localScale = entity.Transform.LocalScale.ToUnity();

            NitroxEntity.SetNewId(gameObject, entity.Id);
            CrafterLogic.NotifyCraftEnd(gameObject, techType);

            if (parent.HasValue && !parent.Value.GetComponent<LargeWorldEntityCell>())
            {
                LargeWorldEntity.Register(gameObject); // This calls SetActive on the GameObject
            }
            else if (gameObject.GetComponent<LargeWorldEntity>() != null && gameObject.transform.parent == null)
            {
                gameObject.transform.SetParent(cellRoot.liveRoot.transform, true);
                LargeWorldEntity.Register(gameObject);
            }
            else
            {
                gameObject.SetActive(true);
            }

            if (parent.HasValue)
            {
                gameObject.transform.SetParent(parent.Value.transform, true);
            }

            Optional<EntityMetadataProcessor> metadataProcessor = EntityMetadataProcessor.FromMetaData(entity.Metadata);

            if (metadataProcessor.HasValue)
            {
                metadataProcessor.Value.ProcessMetadata(gameObject, entity.Metadata);
            }

            result.Set(Optional.Of(gameObject));
            yield break;
        }

        private IEnumerator CreateGameObject(TaskResult<GameObject> result, TechType techType, string classId)
        {
            IPrefabRequest prefabRequest = PrefabDatabase.GetPrefabAsync(classId);
            yield return prefabRequest;

            GameObject prefab;

            if (!prefabRequest.TryGetPrefab(out prefab))
            {
                CoroutineTask<GameObject> techPrefabCoroutine = CraftData.GetPrefabForTechTypeAsync(techType, false);
                yield return techPrefabCoroutine;

                prefab = techPrefabCoroutine.GetResult();

                if (prefab == null)
                {
                    result.Set(Utils.CreateGenericLoot(techType));
                }
            }

            result.Set(Utils.SpawnFromPrefab(prefab, null));
        }

        public bool SpawnsOwnChildren()
        {
            return false;
        }
    }
}
