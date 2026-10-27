# CPUの検証

`CpuBattleChecks.cs`はUnityエディターで実行する独立した検証コードです。通常のゲームコードには含めないため、Assetsの外に置いています。

1. Unity 6000.4.11f1で空の2D検証プロジェクトを作成する。作業中のゲームプロジェクトとは別のフォルダーを使用する。
2. ゲームのAssets/Script、Assets/Data、Assets/Resourcesを、.metaを含めて検証プロジェクトのAssetsへコピーする。
3. TextMesh Pro / Unity UIを利用できる状態にし、CpuBattleChecks.csを検証プロジェクトのAssets/Editorへコピーする。
4. 検証プロジェクトを閉じ、次の引数でUnityを起動する。

```text
Unity.exe -batchmode -nographics -projectPath "検証プロジェクトの絶対パス" -executeMethod CpuBattleChecks.Run -logFile "ログの絶対パス"
```

検証はPlayモードに入り、結果を検証プロジェクトのcpu-checks-result.txtとログへ書き出してUnityを終了します。成功時は`PASS`と項目数、失敗時は例外を記録します。Consoleのログでも失敗がないことを確認してください。

対象はデッキ上限・同名上限、コスト、手札上限、難易度別の合法行動、効果処理の対称性、先攻・後攻の初回ドロー、防御、捨て札選択、予約自傷、山札切れ、設定アセットの参照、演出終了時の非表示です。コルーチンを進めて処理結果を検証しているため、実時間の演出速度、画面レイアウトや見やすさは実際のBattleシーンで別途確認してください。
