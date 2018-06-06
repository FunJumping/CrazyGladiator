
var NeoTest;
(function (NeoTest) {
    class TestApi {
        constructor() {
            this.nodeUrl = "http://10.1.6.48:10332/";
            this.neoLib = new NeoLib();
        }

        init() {
            Debug.output(Debug.INFO, "nftHash: " + this.neoLib.nftHash);
            Debug.output(Debug.INFO, "auctionHash: " + this.neoLib.auctionHash);
            

            $("#getGenGladitorCount").click(function () {
                this.neoLib.getGenGladitorCount(function (err, res) {
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            $("#getGladitorByID").click(function () {
                var inputTokenId = $("#glaIDInput").val();
                this.neoLib.getGladitorByID(inputTokenId, function (err, res) {
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));
			
            $("#ownerOfByID").click(function () {
                var inputTokenId = $("#inputOwnerGla").val();
                this.neoLib.ownerOfByID(inputTokenId, function (err, res) {
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            $("#btnGetAuctionAddr").click(function () {
                this.neoLib.getAuctionAddr( function (err, res) {
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));
			
            $("#btnSetAuctionAddr").click(function () {
                var inputAuctionAddr = $("#inputAuctionAddr").val();
                this.neoLib.setAuctionAddr(inputAuctionAddr, function (err, res) {
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            $("#TokenOfOwnerByIndex").click(function () {
                var inputOwner = $("#inputTokenOfOwnerByIndexOwner").val();
                var inputIndex = $("#inputTokenOfOwnerByIndexIndex").val();
                this.neoLib.TokenOfOwnerByIndex(inputOwner, inputIndex, function (err, res) {
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            // 创建拍卖
            $("#createSaleAuction").click(function() {
                var glaId = $("#inputCreateSaleId").val();
                var startPrice = $("#inputCreateStartPrice").val();
                var endPrice = $("#inputCreateEndPrice").val();
                var duration = $("#inputCreateDuration").val();
                Debug.output(Debug.FUN, "createSaleAuction(" + glaId + ", " + startPrice + ", " + endPrice + ", " + duration + ", callFun)");
                if(duration <= 0) {
                    Debug.output(Debug.ERR, "The duration must be greater then 0");
                    return ;
                }
                if(startPrice <= 0) {
                    Debug.output(Debug.ERR, "The start price must be greater then 0");
                    return ;
                }
                
                this.neoLib.createSaleAuction(glaId, startPrice, endPrice, duration, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            // 创建Clone拍卖
            $("#createCloneAuction").click(function() {
                var glaId = $("#inputCreateCloneSaleId").val();
                var startPrice = $("#inputCreateCloneStartPrice").val();
                var endPrice = $("#inputCreateCloneEndPrice").val();
                var duration = $("#inputCreateCloneDuration").val();
                Debug.output(Debug.FUN, "createCloneAuction(" + glaId + ", " + startPrice + ", " + endPrice + ", " + duration + ", callFun)");
                if(duration <= 0) {
                    Debug.output(Debug.ERR, "The duration must be greater then 0");
                    return ;
                }
                if(startPrice <= 0) {
                    Debug.output(Debug.ERR, "The start price must be greater then 0");
                    return ;
                }
                
                this.neoLib.createCloneAuction(glaId, startPrice, endPrice, duration, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            // 
            $("#buyOnAuction").click(function () {
                var bidIDInput = $("#bidIDInput").val();
                
                Debug.output(Debug.FUN, "getAuction");
                this.neoLib.buyOnAuction(bidIDInput, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            // 
            $("#cloneOnAuction").click(function () {
                var bidMyGlaId = $("#bidMyGlaId").val();
                var bidCloneId = $("#bidCloneId").val();
                
                Debug.output(Debug.FUN, "cloneOnAuction");
                this.neoLib.cloneOnAuction(bidMyGlaId, bidCloneId, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            // 
            $("#btnIsReadyToBreed").click(function () {
                var inputBreedId = $("#inputBreedId").val();
                
                Debug.output(Debug.FUN, "isReadyToBreed");
                this.neoLib.isReadyToBreed(inputBreedId, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            // 
            $("#btnIsPregnant").click(function () {
                var inputPregnantId = $("#inputPregnantId").val();
                
                Debug.output(Debug.FUN, "isPregnant");
                this.neoLib.isPregnant(inputPregnantId, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            // 
            $("#giveBirth").click(function () {
                var inputBirthId = $("#inputBirthId").val();
                
                Debug.output(Debug.FUN, "inputBirthId");
                this.neoLib.giveBirth(inputBirthId, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            // 获取拍卖信息
            $("#getAuctionById").click(function () {
                var inpuTokenId = $("#getAuctionTokenId").val();
                
                Debug.output(Debug.FUN, "getAuctionById");
                this.neoLib.getAuctionById(inpuTokenId, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            $("#btnMintToken").click(function () {
                var inputOwner = $("#inputOwner").val();
                var tokenData = $("#tokenData").val();
				var gene = this.neoLib.decodeGenes(tokenData);
				
                this.neoLib.mintToken(inputOwner, gene.strength, gene.power, gene.agile, gene.speed,
                    gene.skill1, gene.skill2, gene.skill3, gene.skill4, gene.skill5, gene.equip1, gene.equip2, gene.equip3, gene.equip4,
                    gene.restrictAttribute, gene.character, gene.clothes2C, gene.clothes1C, gene.hairLipC, gene.browContourC, gene.eyeballC,
                    gene.nudeC, gene.shoes, gene.knees, gene.pants, gene.belt, gene.chest, gene.bracer, gene.shoulder,
                    gene.face, gene.lip, gene.nose, gene.eyes, gene.hair,
                     function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            $("#createGen0Auction").click(function () {
                var tokenData = $("#gen0TokenData").val();
				var gene = this.neoLib.decodeGenes(tokenData);
				
                this.neoLib.createGen0Auction(gene.strength, gene.power, gene.agile, gene.speed,
                    gene.skill1, gene.skill2, gene.skill3, gene.skill4, gene.skill5, gene.equip1, gene.equip2, gene.equip3, gene.equip4,
                    gene.restrictAttribute, gene.clothes2C, gene.clothes1C, gene.hairLipC, gene.browContourC, gene.eyeballC,
                    gene.nudeC, gene.shoes, gene.knees, gene.pants, gene.belt, gene.chest, gene.bracer, gene.shoulder,
                    gene.face, gene.lip, gene.nose, gene.eyes, gene.hair, gene.character,
                     function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            $("#btnSetSgas").click(function () {
                var sgasHash = $("#sgasHash").val();
                this.neoLib.setSgas(sgasHash, function (err, res) {
					console.log("res:", res);
                    //Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));
			
            $("#Approve").click(function () {
                var inputOwner = $("#ApproveOwner").val();
                var tokenId = $("#ApproveTokenId").val();
                
                this.neoLib.approve(inputOwner, tokenId, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));
            
            $("#btnTransfer").click(function () {
                var TransferFrom = $("#TransferFrom").val();
                var TransferTo = $("#TransferTo").val();
                var TransferTokenId = $("#TransferTokenId").val();
                
                this.neoLib.transfer(TransferFrom, TransferTo, TransferTokenId, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));
            $("#btnTransferFrom").click(function () {
                var TransferFrom = $("#TransferFromFrom").val();
                var TransferTo = $("#TransferFromTo").val();
                var TransferTokenId = $("#TransferFromTokenId").val();
                this.neoLib.transferFrom(TransferFrom, TransferTo, TransferTokenId, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));
            $("#Allowance").click(function () {
                var inputTokenId = $("#AllowanceTokenId").val();
                this.neoLib.allowance(inputTokenId, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));
            
            $("#TokenURI").click(function () {
                var TokenURITokenId = $("#TokenURITokenId").val();
                this.neoLib.tokenURI(TokenURITokenId, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            $("#btnMintTokenNep").click(function(){
                var inputCount = $("#inputMintCount").val();

                this.neoLib.mintTokenNep(inputCount, function(err, res) {
                    console.log(res);
                });
            }.bind(this));

            $("#balanceOfNep").click(function(){
                var inputBalanceOfAddr = $("#inputBalanceOfAddr").val();
                this.neoLib.balanceOfNep(inputBalanceOfAddr, function (err, res) {
                    console.log(res + "");
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));
 
            $("#btnTransferNep").click(function(){
                var transferNepFrom = $("#transferNepFrom").val();
                var transferNepTo = $("#transferNepTo").val();
                var transferNepNumber = $("#transferNepNumber").val();

                this.neoLib.transferNep(transferNepFrom, transferNepTo, transferNepNumber, function(err, res) {
                    console.log(res);
                });
            }.bind(this));

            $("#transferNepToAuc").click(function(){
                var transferNepFrom = $("#transferNepFrom").val();
                var transferNepTo = $("#transferNepTo").val();
                var transferNepNumber = $("#transferNepNumber").val();

                this.neoLib.transferNepToAuc(transferNepFrom, transferNepTo, transferNepNumber, function(err, res) {
                    console.log(res);
                });
            }.bind(this));

            $("#btnRecharge").click(function(){
                var inputRechargeOwner = $("#inputRechargeOwner").val();
                var inputTxid = $("#inputTxid").val();

                this.neoLib.rechargeToken(inputRechargeOwner, inputTxid, function(err, res) {
                    console.log(res);
                });
            }.bind(this));

            $("#btnDrawToken").click(function(){
                var inputDrawOwner = $("#inputDrawOwner").val();
                var inputDrawCount = $("#inputDrawCount").val();

                this.neoLib.drawToken(inputDrawOwner, inputDrawCount, function(err, res) {
                    console.log(res);
                });
            }.bind(this));

            $("#btnBalanceOfAuc").click(function(){
                var inputBlanceOwner = $("#inputBlanceOwner").val();

                this.neoLib.balanceOfAuc(inputBlanceOwner, function(err, res) {
                    console.log(res + "");
                });
            }.bind(this));


            $("#btnBreedWithMy").click(function(){
                var breedWithMyOwner = $("#breedWithMyOwner").val();
                var breedWithMyMother = $("#breedWithMyMother").val();
                var breedWithMyFather = $("#breedWithMyFather").val();

                this.neoLib.breedWithMy(breedWithMyOwner, breedWithMyMother, breedWithMyFather, function(err, res) {
                    console.log(res + "");
                });
            }.bind(this));

            $("#drawToContractOwner").click(function () {
                var inputDrawToCount = $("#inputDrawToCount").val();
                
                this.neoLib.drawToContractOwner(inputDrawToCount, function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));
            
            $("#totalSupplyAuct").click(function () {
                this.neoLib.totalSupplyAuct(function (err, res) {
                    console.log(res);
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));
            
            $("#setAttrConfig").click(function () {
                var inputAttr1 = $("#inputAttr1").val();
                var inputAttr2 = $("#inputAttr2").val();
                var inputAttr3 = $("#inputAttr3").val();
                var inputAttr4 = $("#inputAttr4").val();
                var inputAttrMax0 = $("#inputAttr0Max").val();
                var inputAttrMax1 = $("#inputAttr1Max").val();
                var inputAttrMax2 = $("#inputAttr2Max").val();
                var inputAttrMax3 = $("#inputAttr3Max").val();
                var inputAttrMax4 = $("#inputAttr4Max").val();
                var inputAttrMax5 = $("#inputAttr5Max").val();
                var inputAttrMax6 = $("#inputAttr6Max").val();
                var inputAttrMax7 = $("#inputAttr7Max").val();
                var inputAttrMax8 = $("#inputAttr8Max").val();
                this.neoLib.setAttrConfig(inputAttr1, inputAttr2, inputAttr3, inputAttr4,
                    inputAttrMax0, inputAttrMax1, inputAttrMax2, inputAttrMax3, inputAttrMax4, inputAttrMax5, inputAttrMax6, inputAttrMax7, inputAttrMax8, 
                    function (err, res) {
                    Debug.output(Debug.INFO, "res:" + res);
                });
            }.bind(this));

            
            $("#btnEncode").click(function () {
                var obj = {};
                
                for(var i=0; i<aryAttrKey.length; ++i) {
                    var key = aryAttrKey[i];
                    
                    if($("#" + key) ) {
                        obj[key] = $("#" + key).val();
                    }
                }
                
                var strAttrs = this.neoLib.encodeGenes(obj);
                $("#inputGene").val(strAttrs);
                
            }.bind(this));
            
            $("#btnDecode").click(function () {
                var inputGene = $("#inputGene").val();
                var attrs = this.neoLib.decodeGenes(inputGene);
                //output(INFO, 'gladitor genes attrs: ' + JSON.stringify(attrs));
                //output(INFO, 'gladitor regen genes: ' + cgWeb3.encodeGenes(attrs));
                
                for(var i=0; i<aryAttrKey.length; ++i) {
                    var key = aryAttrKey[i];
                    
                    if($("#" + key)) {
                        $("#" + key).val(attrs[key]);
                    }
                }
                
            }.bind(this));
            
            
        }
        
    }
    window.onload = () => {
        var main = new TestApi();
        main.init();
    };
})(NeoTest || (NeoTest = {}));
