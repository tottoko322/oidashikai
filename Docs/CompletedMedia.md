# 完成素材の設定

- 線画交換会: `Assets/image/Sengakoukan.png`を提供された完成画像へ差し替え。
  CardDataのSprite参照も全体画像に更新し、手札・拡大・図鑑で共通使用する。
- あそびかた: 完成版7枚を`Assets/image/RuleSlide`へ配置。
  RuleシーンのImage 1〜4とAdditional Pages（5〜7）に順番に設定。
  既存の左右矢印、閉じるボタン、縦横比を維持した表示を継続する。
- タイトルBGM: 提供された完成版.wavをそのまま`Assets/Audio/TitleTheme.wav`へ保存。
  titleシーンのAudioManagerのBGM Clipを変更。75秒、44.1kHz、ステレオ。
  音量設定、ループ、図鑑・あそびかた等への移動中の再生継続は既存処理を使用する。
  対戦用の音源は変更しない。

検証用のUnity 6000.4.11f1で`MediaUpdateChecks.Run`を実行。
シーンの7枚の順序・タイトル音源・線画交換会の参照を13項目で確認。
再生中は7枚の表示・往復・新音源の読み込みとループ再生を12項目で確認。
全7枚の実レンダリングを出力し、長い説明も画面内に収まることを確認。
アップロードするバイナリは提供元と同一のGit SHAであることを照合する。
