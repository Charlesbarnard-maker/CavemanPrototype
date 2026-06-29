using UnityEngine;

namespace Caveman
{
    /// <summary>Re-skins the player to the age-appropriate travel mount as the colony advances ages:
    /// On Foot (the caveman) → Horseback → Ox Cart → Wagon → Motorbike — mirroring the speed tiers in
    /// <see cref="PlayerController"/>. Pure cosmetic — art is generated in <see cref="PlaceholderArt.PlayerMount"/>
    /// (age 0 falls through to <see cref="PlaceholderArt.Caveman"/>). On water with a boat, shows the boat.</summary>
    public class PlayerAvatar : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private bool _onBoat;
        private int _frame;
        private float _animT;
        private Vector3 _lastPos;
        private int _shownKey = -2; // age*3 + frame currently displayed (−2 = none/boat)

        void Awake() { _sr = GetComponent<SpriteRenderer>(); _lastPos = transform.position; }

        void Update()
        {
            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            // On water WITH a boat → show the boat; otherwise the age-appropriate caveman (animated).
            bool onWater = PlayerController.HasBoat
                && TerrainGrid.IsWater(new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y)));
            if (onWater)
            {
                if (!_onBoat && _sr != null) { _sr.sprite = PlaceholderArt.Boat(); _sr.color = Color.white; }
                _onBoat = true; _shownKey = -2; _lastPos = transform.position;
                return;
            }
            _onBoat = false;
            // Walk cycle while moving; stand on frame 0 when still.
            bool moving = (transform.position - _lastPos).sqrMagnitude > 0.00004f;
            _lastPos = transform.position;
            if (moving) { _animT += Time.deltaTime; if (_animT >= 0.14f) { _animT = 0f; _frame = (_frame + 1) % 3; } }
            else { _frame = 0; _animT = 0f; }
            int key = age * 3 + _frame;
            if (key != _shownKey && _sr != null) { _shownKey = key; _sr.sprite = PlaceholderArt.PlayerMount(age, _frame); _sr.color = Color.white; }
        }
    }
}
