using UnityEngine;

namespace Caveman
{
    /// <summary>A cosmetic machinery overlay that bolts tech onto a building's upper-right and GROWS with its
    /// upgrade tier — a bronze gear (tier 1) → an iron gear + piston (tier 2) → a smokestack + glowing power
    /// panel (tier 3). Reads the building's <c>Tier</c> live each frame, so a building visibly mechanises the
    /// instant you upgrade it. Parented to the building (auto-cleaned on demolish); art in
    /// <see cref="PlaceholderArt.TierMachinery"/>. Purely visual.</summary>
    public class MachineUpgradeFX : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private System.Func<int> _tier;
        private int _frame; private float _animT; private int _shownTier = -99; private int _shownFrame = -1;

        public static MachineUpgradeFX Attach(Transform building, System.Func<int> tier)
        {
            var go = new GameObject("UpgradeMachinery");
            go.transform.SetParent(building, false); // inherit the building's position + footprint scale
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 6; // above the building body (5)
            var fx = go.AddComponent<MachineUpgradeFX>();
            fx._sr = sr; fx._tier = tier;
            return fx;
        }

        void Update()
        {
            int tier = _tier != null ? _tier() : 0;
            if (tier <= 0) { if (_sr != null && _sr.enabled) _sr.enabled = false; return; }
            if (_sr == null) return;
            _sr.enabled = true;
            _animT += Time.deltaTime;
            if (_animT >= 0.15f) { _animT = 0f; _frame = (_frame + 1) % 4; }
            if (tier != _shownTier || _frame != _shownFrame)
            {
                _shownTier = tier; _shownFrame = _frame;
                _sr.sprite = PlaceholderArt.TierMachinery(tier, _frame);
                _sr.color = Color.white;
            }
        }
    }
}
