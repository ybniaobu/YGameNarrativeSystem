# 错误输入示例

故意写满错误的示例数据，用来验证 `YDialogueValidator` 的报错输出。

```
错误输入格式/
├── global/
│   └── characters.csv      重复 ID、空 ID、未被使用的角色
└── chapter/
    ├── chapter01/          大多数节点级错误
    ├── chapter02/          没有 end 节点
    ├── chapter03/          chapter_id 与 chapter01 重复
    └── chapter04/          nodes.csv 只有表头
```

## 期望结果

**读取**：通过（文件都能读、列都在）。

**校验**：10 条错误、2 条警告。

| 来源 | 问题 |
|---|---|
| characters.csv | `speaker_id 重复：spk_001_lin` |
| characters.csv | `speaker_id 为空` |
| ch01 | `入口节点不存在：ch01_0001` |
| ch01 | `node_id 为空` |
| ch01 | `node_id 重复：ch01_0003` |
| ch01 | `type 不合法：lin` |
| ch01 | `line 必须仅有 1 条分支，实际 2 条` |
| ch01 | `choice 至少需要 1 条分支，实际 0 条` |
| ch01 | `end 不应有分支，实际 1 条` |
| ch01 | `分支目标不存在：ch01_9999` |
| ch01 | `分支目标重复：ch01_0009` |
| ch01 | `node_id 前缀不是 ch01：ch02_0001` |
| ch01 | `speaker_id 不存在：spk_999_ghost` |
| ch01 | `zh_cn 不能为空` |
| ch02 | `没有 end 节点` |
| chapter03 | `chapter_id 重复：ch01` |
| ch04 | `章节没有任何节点` |
| 警告 | `缺少 en_us`（ch01_0011） |
| 警告 | `speaker_id 没有被任何节点使用：spk_004_unused` |
