StyleCopSVNHook
===============

以下の記事とリポジトリのソースを元に作っています。
オリジナルにライセンスは明記されていません。

- http://www.codeproject.com/Articles/748836/Check-StyleCop-Rules-on-TortoiseSVN-Commit
- https://github.com/gertwallis/StyleCopSVNHook 


無視ファイルリストを作る
------------------------

既存のプロジェクトでスタイルチェックを始めると、膨大なエラーが出力されてつらいので、既存のファイルは解析対象外にして始めるとよいです。

```
StyleCopSVNHook -l ディレクトリ名 > StyleCopIgnore.txt
``` 
で、指定ディレクトリ以下の .cs ファイルのパスを StyleCopIgnore.txt に列挙します。


TortoiseSVN の Pre-Commit Hook
------------------------------

TortoiseSVN の設定ダイアログを開き、 Hook Scripts で Pre-Commit Hook に StyleCopSVNHook を追加してください。


Unity Editor での自動チェック
------------------------------

未実装ですが、 AssetPostprocessor.OnPostprocessAllAssets で StyleCopSVNHook を呼び出せばよさそう。
