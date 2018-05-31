# CrazyGladiator
#### 合约说明
角斗士项目中总共使用三个合约：sgas，NFT和Auction，其中Sgas合约为NEL开发的通用合约，NFT和Auction 为我们自己开发。
NFT合约管理角斗士资源，以及克隆操作；
Auction合约管理角斗士的拍卖，出租，购买和手续费扣取。
本项目所用合约均使用C#开发。
1.	Sgas.cs
作用：用于gas 和sgas之间1：1兑换。
由于合约里直接操作gas比较困难，所以需要玩家先将gas通过sgas合约转换为NEP5资产，合约里操控的是sgas资产，这样合约写起来会比较方便。
2.	NFT.cs
作用：管理角斗士资源，以及克隆操作。
3.	Auction.cs
作用：管理角斗士的拍卖，出租，购买和手续费扣取。
