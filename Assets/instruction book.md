# スクリプト説明
分からないことがあれば聞いて

## データ定義（DataDefinition / 設定）

- CardData（攻/防/コスト/アート/効果IDなど）
- EffectData（効果タイプ、参照方式、倍率/貫通フラグ、選択ルールなど）
- DeckDefinition（配布デッキ：カードIDと枚数）
- CharacterData（ピコ/ナノ：立ち絵、コイン面の先攻条件など）
- GameConfig（手札上限8、デッキ上限15、同名2、Confirm ON/OFF、初期手札4 等）

## タイトル・モード遷移・保存(Title)

- TitleMenuController（配布/構築/ルールの3ボタン）
- ModeContext（配布 or 構築のモード保持）
- SceneFlowManager（タイトル→キャラ選択→バトル等）
- RulePanelController（ルール表示）
- SaveManager（PlayerPrefs：音量、Confirm設定、最後のキャラ等）

## キャラ選択（横スライド）・コイントス（スキップ不可）(Character&Coin)

- CharacterSelectManager
- CarouselController（横スライド＋スナップ）
- CharacterSelectUI（表示更新）
- CoinTossController（演出＋結果）
- CoinUI（Q/I表示、回転、結果表示）
- FirstTurnDecider（キャラ×結果→先攻判定）

## バトル中枢（状態機械 / 進行 / 勝敗）(BattleManager)

- BattleStateMachine（ターン進行・フェーズ管理）
- TurnSystem（先攻後攻、ターン開始/終了）
- WinLoseSystem（勝敗判定、遷移指示）
- InputLockManager（ロジック停止：音はOK）

## リソース・山札・手札（ゲームの骨格）(Deck&Hand)

- CostManager（max5、start0、毎ターン+1、効果で増減）
- DeckManager（山札/捨て札/ドロー、ドロー時0なら敗北）
- HandManager（手札8固定、ドラッグ除外、手札状態管理）
- HandLayoutController（中央寄せ、縮小、当たり判定、ホバー押し出し）
- HandRootMover（ドラッグで手札全体を下げる）
- CardDealAnimator（初期4枚シュッ×4、通常ドロー演出）

## カード表示・入力（ホバー/クリック/ドラッグ/ドロップ）(Card)

- CardView（見た目更新）
- CardInteraction（クリック→ポップアップ、ドラッグ開始/終了）
- CardHoverNotifier（ホバーEnter/ExitをHandLayoutへ通知）
- DragDropController（追従、Raycast、ドロップ判定）
- DropZone（EnemyZone/EffectZone 受け取り）
- ZoneHighlighter（攻撃/効果ゾーンのハイライト）
- CardPopupUI（クリックで開閉、再クリックで閉じる）
- ConfirmController（ON/OFF切替：実行/戻す）

## 解決系（攻撃/防御/効果/状態異常）(Effect)

- ResolveManager（解決順、演出待ち、キュー管理）
- DamageCalculator（atk-def、倍率、貫通、加算を吸収）
- EffectSystem（EffectData実行の中心）
- StatusSystem（呪い/継続：TurnEnd前に処理）
- TimingSystem（TurnStart/TurnEnd前などのトリガ）
- RestrictionSystem（効果禁止など）

## 防御フェーズ(Defense)

- DefenseSelectUI（防御カード選択 or スキップ必須）
- TurnEndButtonController（「何もしない」時だけ押す、文言切替）

## 選択効果(Select)

- SelectionController（選択待ちでResolveを止める）
- CardSelectUI（手札/捨て札など候補表示、キャンセル可）
- SelectionRule（EffectData内のデータ構造でも可）

## CPU（AI）(CPU)

- EnemyAI
    - 防御：最大防御、同値なら攻撃低い方
    - 攻撃/効果の選択ロジック（後で強化可）

## 演出(enshutu)

- CharacterHitVfx（防御後に上下揺れ＋赤フラッシュ）
- CardVanishVfx（ドロップ確定でワープ消滅）
- TurnBannerController（Battle Start/先攻後攻/ターン表示）

## 音・設定(Audio)

- AudioManager（BGM/SE別）
- SettingsModalUI（音量スライダー、保存）