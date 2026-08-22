# Project: OVERRIDE

<div align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3.14f1-000000?style=flat-square&logo=Unity&logoColor=white"/>
  <img src="https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white"/>
</div>

<br>

> **"작전명(Operation) 오버라이드. 에코 에너지의 폭주로 잠식된 지하 시설을 탐험하고 시스템의 제어권을 탈환하라."**
> 
> 기획자의 생산성을 높이는 데이터 주도 설계(Data-Driven)와 비동기 연출 동기화에 집중한 2D 턴제 RPG 클라이언트 코어 아키텍처입니다.
> *본 레포지토리는 상용 에셋 저작권 보호를 위해 **순수 C# 스크립트(Scripts) 코드만 업로드된 포트폴리오용 레포지토리**입니다.*

<br>

## 게임 시연 (Play Video)
![Game_Play_GIF](https://github.com/user-attachments/assets/24104594-c078-40cf-a2d5-3e416cfab08c)

**[실행 가능한 빌드 다운로드](https://drive.google.com/file/d/19fVRdilDRetZowMmNxJd2gndj8wpSOgp/view)** (개발 중 빌드로 일부 버그가 존재할 수 있습니다.)

<br>

## 📋 프로젝트 개요 (Overview)
| 항목 | 내용 |
| --- | --- |
| **개발 기간** | 2026.05 ~ 현재 (진행 중) |
| **장르** | 2D 플랫포머 탐험 + 깊이 있는 속성/상태이상 연계 턴제 RPG |
| **팀 구성** | 5명 (클라이언트 프로그래머 2명, 기획자 2명, 원화 1명) |
| **담당 역할** | **Main Client Programmer**<br>- 전투 엔진 및 로직 아키텍처 설계<br>- 가중치 기반 유틸리티 AI 구현<br>- DataManager 전역 데이터 파이프라인 구축<br>- 물리 연산(조작감) 및 객체 지향 상호작용 구현 |

<br>

## 핵심 구현 및 트러블슈팅 (Core Highlights)

단순한 기능 구현을 넘어, '확장성'과 '안정성'을 최우선으로 고려하여 설계했습니다. 자세한 코드는 링크된 스크립트와 주석을 통해 확인하실 수 있습니다.

### 1. 전투 로직/연출 분리 및 엣지 케이스(Edge Case) 방어
- **문제 상황:** 다단 히트 스킬 연출 도중 타겟의 체력이 변하거나 사망하면 타겟팅 조건이 실시간으로 변동되어 엉뚱한 대상을 타격하거나 데미지가 증식하는 치명적인 상태 동기화 문제 발생.
- **해결 방안 (`Target Snapshot & Filtering` 패턴):** 
  - 수치 연산 전담(`BattleLogicHandler`)과 시각적 연출 전담(`CharacterAction`)으로 **관심사를 완전 분리**.
  - 스킬 시전 시점의 타겟 명세서를 **스냅샷(Snapshot)**으로 캡처하고, 실제 연출 적중 시점에 교집합을 필터링하여 데미지를 적용.
  - 대상에게 걸린 '저주(Curse)' 상태이상 판별 시 힐이 데미지로 변환되는 기믹 등 턴제 특유의 엣지 케이스를 안전하게 분기 처리.
- **📁 관련 코드:** [`BattleLogicHandler.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Battle/BattleLogicHandler.cs) / [`CharacterAction.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Character/CharacterAction.cs)

### 2. 데이터 주도 설계(Data-Driven)와 가중치 기반 유틸리티 AI
- **동적 스킬 파이프라인:** 단타, 연타 등 복잡한 스킬 메커니즘을 `SkillData` 및 `EffectPayload` (ScriptableObject) 내부에 정의하여 모듈화. 프로그래머 개입 없이 스킬 조합 가능.
- **핑퐁 패턴(Ping-Pong Pattern) 극복:** 마나가 특정 수치일 때 스킬과 방어를 기계적으로 반복하는 단순 AI 패턴의 단조로움을 극복.
- **가중치 기반 연산:** 타겟의 현재 체력, 획득한 방어 버프, 도발 상태 등을 실시간으로 종합 평가하여 **행동 점수(Score)**를 산출하는 가중치 기반 AI 아키텍처 구축.
- **📁 관련 코드:** [`EffectSystem.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Character/EffectSystem.cs) / [`EnemyAI.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Character/Enemy/EnemyAI.cs)

### 3. 전역 데이터 파이프라인 및 이벤트 주도(Event-Driven) 시스템
- **전투 씬 분리 및 데이터 전달:** 전투 씬은 1개만 구성하고, 필드의 몬스터가 전투를 발생시킬 때 `DataManager`에 `BattleStageData`를 넘겨 배경을 동적으로 렌더링하도록 징검다리 파이프라인 구축.
- **이벤트 주도(Event-Driven) 대사 및 UI:** UI 갱신 시 `GameEvents`를 통한 구독 방식을 도입. 대화창 시스템은 '다음 대사 넘기기'와 '연출 스킵(즉시 출력)'을 `Z`키 단일 입력으로 제어해 직관적인 UX를 달성.
- **📁 관련 코드:** [`DataManager.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Data/DataManager.cs) / [`DialogueManager.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Cutscene/DialogueManager.cs)

### 4. 객체 지향적 퍼즐/기믹 아키텍처 (Object-Oriented Interaction)
- **상태 동기화 및 캡슐화:** 필드에 배치된 카드, 잠긴 문 등의 기믹은 씬 시작 시 `DataManager`를 통해 고유 이름(ID) 기반으로 과거 상태를 검사하여 파괴되거나 문이 열려 있도록 처리. 
- 특정 유물(Artifact) 획득 여부를 `DataManager`에서 관리하고 벽점프(`FieldPlayerWallJump`), 더블점프(`FieldPlayerDoubleJump`) 등 스크립트에서 이를 동기화.
- **📁 관련 코드:** [`InteractObject.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Field/Interact/InteractObject.cs) / [`FieldCard.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Field/Interact/FieldCard.cs) / [`FieldRelic.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Field/Interact/FieldRelic.cs) / [`FieldPlayerWallJump.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Field/Player/FieldCharacterAbility/FieldPlayerWallJump.cs)

### 5. 캐릭터 성장 및 확률 기반 드랍 파이프라인 (Stat & Reward System)
- **드랍 아이템 데이터 구조화:** `DropItemData` 구조체에 아이템 ID뿐만 아니라 `dropChance`(드랍 확률), 최소/최대 수량, 해당 아이템이 떨어지기 시작하는 `minLevelRequired`(몬스터 최소 레벨) 변수를 추가하여 확정 및 확률 드랍이 혼재된 테이블 구축.
- **보상 자동 합산:** 몬스터의 1레벨 기본 제공 경험치 및 골드와 성장 스탯을 엑셀처럼 직관적으로 세팅. 몬스터의 레벨이 변경될 때마다 획득 경험치, 골드, 드랍 풀이 `EncounterReward`로 자동 계산되는 시스템 구축.
- **📁 관련 코드:** [`CharacterData.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Data/CharacterData.cs) / [`FieldMonster.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Field/Enemy/FieldMonster.cs)

### 6. 타임라인 연산 및 렌더링 파이프라인 최적화 (Optimization & Architecture)
- **Dirty Flag 패턴 기반 턴 예측(Timeline) 연산 최적화:** 모든 캐릭터의 속도와 현재 행동 게이지를 바탕으로 미래의 턴 대기열(N바퀴)을 예측·정렬하는 무거운 연산 병목을 최적화했습니다. 턴 강제 할당(새치기)이나 광역 도트 데미지로 게이지가 1프레임 내에 연쇄적으로 변하더라도 즉시 계산하지 않고 `isTimelineDirty` 플래그만 활성화하여, `LateUpdate`에서 단 1회만 정렬 연산을 수행하도록 압축해 프레임 드랍을 차단했습니다.
- **유니티 코루틴과 물리 엔진의 충돌(로딩 스파이크) 트러블슈팅:** Room 전환 시 발생하는 렉을 줄이고자 오브젝트 활성화를 프레임 단위로 쪼개는 비동기 분산 처리(Chunking)를 시도했으나, 바닥 타일이 활성화되기 전 몬스터가 추락하거나 스크립트의 `OnEnable`이 계층 구조를 건드려 코루틴이 강제 종료되는 엔진의 크리티컬한 한계를 직면했습니다. 이를 통해 엔진의 물리 생명주기를 이해하고, 활성화(`SetActive(true)`)는 물리 버그 방지를 위해 즉시(Instant) 처리하되, 이전 방의 비활성화만 `DelayedDeactivateRooms` 코루틴으로 지연시키는 하이브리드 아키텍처로 선회하여 스파이크와 물리 버그를 해결했습니다.
- **물리 틱 동기화 및 시네머신 워프(Warp) 제어:** 
  - 무빙 플랫폼 탑승 시 발생하는 캐릭터 Jittering(떨림) 현상을 물리 틱(`FixedUpdate`)과 렌더링 틱(`Update`)을 일치시키고 `Rigidbody2D.MovePosition`을 활용해 제거.
  - 씬 이동이나 텔레포트 시 시네머신 카메라가 먼 거리를 무리하게 추적하려는 비주얼 버그를 `vcam.OnTargetObjectWarped` 함수 호출로 즉시 스냅(Snap)시켜 공간 제어 달성.
- **📁 관련 코드:** [`TurnCalculator.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Battle/TurnCalculator.cs) / [`RoomManager.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Room/RoomManager.cs) / [`TeleportManager.cs`](https://github.com/124coding/Project-Override_Code_Only/blob/main/Scripts/Field/TeleportManager.cs)
