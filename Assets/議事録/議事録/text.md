# 各スクリプトについての説明

## BattleStateMachien

### `public static BattleStateMachine I { get; private set; }`
- シングルトン。
  - インスタンスが一つしか作れないようなクラスの生成
  - 複数生成を防ぐことが可能
  - グローバルなアクセスポイントを提供する
  - ***staticとは何が違うの?***
    - インスタンス(Singleton)として扱うか関数(Static)として扱うかの違い
    - Singleton:状態を持っていてシステム全体で1つの窓口として動くもの
    - Static:入力に対して答えを出すだけのもの
- どこからでも`BattleStateMachine.I`でアクセス可

### `public GameConfig config;`
