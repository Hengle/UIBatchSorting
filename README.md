# 解决的问题 #

A，C 共用材质1， B使用材质2，Depth顺序为ABC，这时候需要三个DrawCall，如果AB，BC不全部重叠，将Depth顺序调整为ACB（或CAB或BAC等等），则可以将DrawCall降低为两个。

# 如何使用 #

目前支持NGUI的Depth调整，其他UI库可继承自 `class SortItem` 自行扩展。

对于NGUI，选择GameObject，然后点击菜单中的 Batch Sorting 即可。

