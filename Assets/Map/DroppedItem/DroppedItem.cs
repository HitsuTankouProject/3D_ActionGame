using Unity.VisualScripting;
using UnityEngine;

public abstract class DroppedItem : MonoBehaviour
{
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
    protected Renderer modelRenderer;
    protected Vector3 serUpPosition;
    private float elapsedTime;
    private float actualAmplitude;

    protected virtual void SetUp()
    {
        serUpPosition = transform.localPosition;
        elapsedTime = 0f;

        if (modelRenderer == null) return;

        Bounds bounds = modelRenderer.bounds;
        Vector3 bottomPosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        if (Physics.Raycast(bottomPosition, Vector3.down, out RaycastHit floorHit,
            Mathf.Infinity, mapLayer, QueryTriggerInteraction.Ignore))
        {
            actualAmplitude = Mathf.Min(amplitude, floorHit.distance);
        }
    }

    protected virtual void UpAndDown()
    {
        elapsedTime += Time.deltaTime;

        float offset = Mathf.Sin(
            elapsedTime * frequency * Mathf.PI * 2f) * actualAmplitude;

        transform.localPosition = serUpPosition + Vector3.up * offset;
    }


    #endregion

    private void Awake()
    {
        mapLayer = LayerMask.GetMask("Map");
        modelRenderer = GetComponent<Renderer>();
    }


    protected virtual void Start()
    {
        SetUp();
    }

    protected virtual void Update()
    {
        ItemRotation();
        UpAndDown();
        if (IsCloseEnough()) ItemMoveToCharacter();
    }


    protected abstract void ItemReaction(Collider other);
    private void OnTriggerEnter(Collider other)=> ItemReaction(other);




}
