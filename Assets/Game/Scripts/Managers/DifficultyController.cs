using System;
using UnityEngine;

namespace Managers
{
    public sealed class DifficultyController : MonoBehaviour
    {
        [SerializeField] private DifficultyConfig config;

        private float elapsedTime;
        private bool isRunning;

        public event Action<DifficultySnapshot> WaveChanged;
        public DifficultySnapshot Current { get; private set; }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("DifficultyController requires a DifficultyConfig.", this);
                enabled = false;
                return;
            }

            Current = config.GetWave(1);
        }

        private void Update()
        {
            if (!isRunning) return;

            elapsedTime += Time.deltaTime;
            var wave = Mathf.FloorToInt(elapsedTime / config.WaveDuration) + 1;
            if (wave == Current.Wave) return;

            Current = config.GetWave(wave);
            WaveChanged?.Invoke(Current);
        }

        public void ResetDifficulty()
        {
            elapsedTime = 0f;
            isRunning = false;
            Current = config.GetWave(1);
            WaveChanged?.Invoke(Current);
        }

        public void Begin()
        {
            isRunning = true;
        }

        public void Stop()
        {
            isRunning = false;
        }
    }
}
