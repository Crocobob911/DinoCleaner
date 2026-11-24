using System.Collections.Generic;
using DinoCleaner.Core;
using UnityEngine;

namespace DinoCleaner.Stage
{
    // 세차 중에 돌발 상황을 골라서 일으키고, 끝나면 정리한다. 한 번에 하나씩.
    internal sealed class IncidentDirector
    {
        readonly StageController _controller;
        readonly Dictionary<IncidentDefinition, int> _counts = new Dictionary<IncidentDefinition, int>();

        IncidentDefinition _currentDef;
        IStageIncident _current;
        GameObject _currentObject;
        float _nextTime;   // 다음 사건을 시도할 세차 시간

        public IncidentDirector(StageController controller) => _controller = controller;

        // 세차 시작
        public void Begin()
        {
            _counts.Clear();
            ScheduleNext(0f);
        }

        public void Tick(float washTime)
        {
            if (_current != null)
            {
                // 사건이 스스로 끝났거나, 다른 이유로 오브젝트가 사라졌으면 정리
                if (_currentObject == null || _current.IsFinished) Finish(washTime, true);
                return;
            }

            if (washTime >= _nextTime) TryStart(washTime);
        }

        // 세차 끝: 진행 중인 사건 강제 종료
        public void Stop()
        {
            if (_current != null) Finish(0f, false);
        }

        void TryStart(float washTime)
        {
            var def = Pick(washTime);
            if (def == null) { ScheduleNext(washTime); return; }   // 지금 일어날 수 있는 사건 없음

            var washPoint = _controller.WashPoint;
            var go = Object.Instantiate(def.prefab, washPoint.position, washPoint.rotation);
            var incident = go.GetComponent<IStageIncident>();
            if (incident == null)
            {
                Debug.LogError($"[Incident] {def.name}의 프리팹에 IStageIncident 컴포넌트가 없습니다.", def);
                Object.Destroy(go);
                ScheduleNext(washTime);
                return;
            }

            _currentDef = def;
            _current = incident;
            _currentObject = go;
            _counts.TryGetValue(def, out int count);
            _counts[def] = count + 1;

            incident.Begin(new IncidentContext(_controller.Dino, _controller.Progress, washPoint));
            EventBus<IncidentStartedEvent>.Publish(new IncidentStartedEvent(def));
        }

        void Finish(float washTime, bool scheduleNext)
        {
            var def = _currentDef;
            if (_currentObject != null)
            {
                _current.End();
                Object.Destroy(_currentObject);
            }

            _current = null;
            _currentObject = null;
            _currentDef = null;

            EventBus<IncidentEndedEvent>.Publish(new IncidentEndedEvent(def));
            if (scheduleNext) ScheduleNext(washTime);
        }

        // 지금 일어날 수 있는 사건 중 가중치 랜덤으로 하나
        IncidentDefinition Pick(float washTime)
        {
            var list = _controller.Stage.incidents;
            if (list == null) return null;

            float total = 0f;
            foreach (var def in list)
                if (CanStart(def, washTime)) total += def.weight;
            if (total <= 0f) return null;

            float roll = Random.Range(0f, total);
            foreach (var def in list)
            {
                if (!CanStart(def, washTime)) continue;
                roll -= def.weight;
                if (roll <= 0f) return def;
            }
            return null;
        }

        bool CanStart(IncidentDefinition def, float washTime)
        {
            if (def == null || def.prefab == null || def.weight <= 0f) return false;
            if (washTime < def.earliestTime) return false;
            _counts.TryGetValue(def, out int count);
            return def.maxPerStage == 0 || count < def.maxPerStage;
        }

        void ScheduleNext(float washTime)
        {
            var range = _controller.Stage.incidentInterval;
            _nextTime = washTime + Random.Range(range.x, Mathf.Max(range.x, range.y));
        }
    }
}
