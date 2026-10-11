using Unity.VisualScripting;
using UnityEngine;
using Cysharp.Threading.Tasks;

public enum ItemType { None, Weapon, ColorBall }

public abstract class DroppedItem : MonoBehaviour
{
    public abstract ItemType type { get; }
    protected ItemPoolManager _itemPoolManager => ItemPoolManager.Instance;

    #region Move To Character
    protected Transform targetCharacter => GameManager.Instance?.gamePlayer?.controlling_Character?.transform;
    protected virtual float attractionDistance => 3f;
    protected virtual float moveSpeed => 5f;
    protected bool IsCloseEnough()
    {
        if (targetCharacter == null) return false;
        return Vector3.Distance(this.transform.position, targetCharacter.position) < attractionDistance;
    }
    protected virtual void ItemMoveToCharacter()
    => transform.position = Vector3.MoveTowards(transform.position, targetCharacter.position, moveSpeed * Time.deltaTime);

    #endregion

    #region Rotation
    protected virtual Vector3 rotationAxis => Vector3.up;
    protected virtual float rotationSpeed => 180f;

    protected virtual void ItemRotation()
        => transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);

    #endregion

    #region Up And Down
    protected virtual float amplitude => 0.3f;
    protected virtual float frequency => 1f;

    protected LayerMask mapLayer;
    [SerializeField] protected Renderer modelRenderer;
    protected Vector3 setUpPosition;
    private float elapsedTime;
    private float actualAmplitude;

    protected bool setUpDone = false;
    public virtual void SetUp()
    {
        setUpPosition = transform.position;
        elapsedTime = 0f;

        if (modelRenderer == null) return;

        Bounds bounds = modelRenderer.bounds;
        Vector3 bottomPosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        if (Physics.Raycast(bottomPosition, Vector3.down, out RaycastHit floorHit,
            Mathf.Infinity, mapLayer, QueryTriggerInteraction.Ignore))
        {
            actualAmplitude = Mathf.Min(amplitude, floorHit.distance);
        }
        setUpDone = true;
    }

    protected virtual void UpAndDown()
    {
        elapsedTime += Time.deltaTime;

        float offset = Mathf.Sin(
            elapsedTime * frequency * Mathf.PI * 2f) * actualAmplitude;

        transform.position = setUpPosition + Vector3.up * offset;
    }


    #endregion

    private void Awake()
    {
        mapLayer = LayerMask.GetMask("Map");
    }

    protected virtual void Update()
    {
        if (!setUpDone) return;
        ItemRotation();
        UpAndDown();
        if (IsCloseEnough()) ItemMoveToCharacter();
    }

    protected void ReturnToPool() => _itemPoolManager.ReturnToPool(this);
    protected abstract void ItemReaction(Collider other);
    private void OnTriggerEnter(Collider other)=> ItemReaction(other);




}
