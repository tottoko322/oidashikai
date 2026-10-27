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

## キャラクター・難易度の選択

同じ隔離プロジェクトで、BattleSelectionChecks.csをAssets/Editorへ、SelectionFlowSpy.csをAssets直下へコピーしてください。executeMethodを`BattleSelectionChecks.Run`へ変更すると、画像・デッキ・先攻条件・難易度保存・キャンセル・画面遷移の順序・二重遷移の防止を検証します。結果はselection-checks-result.txtへ記録します。テストはPlayerPrefsの難易度キーを終了時に元へ戻します。

SelectionFlowSpyは実際のシーンロードを記録へ置き換える検証専用コンポーネントです。タイトルや対戦の見た目、ボタン操作を通した全体の進行は、元のプロジェクトでも手動確認してください。

## モニターの先攻・後攻演出

TurnOrderSlotChecks.csを隔離プロジェクトのAssets/Editorへコピーし、executeMethodに`TurnOrderSlotChecks.Run`を指定してください。標準のuGUIパッケージとTMP Essential Resourcesを導入した環境を使います。この検証はUnityのコルーチンを実際のフレームで進めます。

停止した文字とターン順の一致、各キャラクターの先攻条件、抽選が1回であること、二重再生、中断後の後始末、時間停止中の進行、画像の差し替え、入力ロック、初期配札より先に演出を完了することを確認し、slot-checks-result.txtへ結果を記録します。最終的な見た目とテンポは元のBattleシーンでも確認してください。

## フォントの統一

MenuMusicRevealChecks.csを隔離プロジェクトのAssets/Editorへコピーし、executeMethodに`MenuMusicRevealChecks.Run`を指定すると、シーンを破棄・生成するBGM継続検証と、実際のフレームでカードを保持する検証を実行します。メニュー間の継続、同曲への復帰と二重再生防止、戦闘曲との切り替え、攻撃値の保持、防御/スキップ選択後の戻りとダメージ計算、通常カードの表示時間を確認します。結果はmusic-reveal-checks-result.txtへ記録します。

AudioPromptChecks.csを隔離プロジェクトのAssets/Editorへコピーし、executeMethodに`AudioPromptChecks.Run`を指定すると、BGM再生開始、音量保存の互換性と同期、設定画面の無通知初期化、防御の操作案内を検証します。音源は生成した検証用クリップを使い、結果はaudio-prompt-checks-result.txtへ記録します。検証前の音量設定は終了時に復元します。

リポジトリ内で`python Tools/Validation/audit_game_fonts.py`を実行すると、各ゲームシーンとカードPrefabのFont Asset・共有マテリアル、TMPの既定フォントを読み取り専用で確認できます。

UnityではGameFontChecks.csを隔離プロジェクトのAssets/Editorへコピーし、executeMethodに`GameFontChecks.Run`を指定します。フォント本体と.meta、共通フォントに変更したTMP Settings.assetも検証用プロジェクトへコピーしてください。Resources内のTMP Settingsは重複させず、変更後の既定フォントが読み込まれる状態にします。結果はfont-checks-result.txtに記録します。
