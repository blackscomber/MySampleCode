using UnityEngine;
using System;
using System.Collections;
using SanGames;
using SanGames.GlobalType;

public class MainHudController : MonoSingleton<MainHudController>
{
	public UILabel RubyLabel;
	public UILabel GameMoneyLabel;
	public DailyEnergy dailyEnergy;

	public UILabel PresentCountLabel;
	public UILabel PresentPointLabel;
	public UISlider PresentPointSlider;
	
	public TweenScale RubyScaleAnimation;
	public TweenScale GameMoneyScaleAnimation;
	
	public UILabel dailyEnergyLabel1;
	public UILabel dailyEnergyLabel2;
	
	public PlayerCharacterInfo playerCharacterInfo;
	public PlayerPetInfo playerPetInfo;
	
	public FriendInfo friendInfo;

	public PetMenus petMenu;
	
	public GameObject tutorialPopup;
	public GameObject gameReadyButton;

	public GameObject lastWeekRankPopup;
	public DailyLoginReward dailyLoginRewardPopup;
	
	PlayerData _playerDataHolder;
	public PlayerData playerDataHolder
	{
		get{			
			if( _playerDataHolder == null )
				_playerDataHolder = DataHolder.Instance.GetData<PlayerData>();
			return _playerDataHolder;
		}
	}
	CharactersData _characterDataHolder;
	public CharactersData characterDataHolder
	{
		get{
			if( _characterDataHolder == null )
				_characterDataHolder = DataHolder.Instance.GetData<CharactersData>();
			return _characterDataHolder;
		}
	}
	PetsData _petDataHolder;
	public PetsData petDataHolder
	{
		get{
			if( _petDataHolder == null )
				_petDataHolder = DataHolder.Instance.GetData<PetsData>();
			return _petDataHolder;
		}
	}
	
	CharacterTableSet _charTableSet;
	public CharacterTableSet charTableSet
	{
		get{
			if( _charTableSet == null )
				_charTableSet = CSVParseManager.Instance.GetTableSet<CharacterTableSet>();
			return _charTableSet;
			
		}
	}
	PetTableSet _petTableSet;
	public PetTableSet petTableSet
	{
		get{
			if( _petTableSet == null )
				_petTableSet = CSVParseManager.Instance.GetTableSet<PetTableSet>();
			return _petTableSet;
		}
	}
	TextTableSet _textTableSet;
	public TextTableSet textTableSet
	{
		get{
			if( _textTableSet == null )
				_textTableSet = CSVParseManager.Instance.GetTableSet<TextTableSet>();
			return _textTableSet;
		}
	}
	
	bool initializing;

	// Use this for initialization
	void Awake () {
		
		#if UNITY_EDITOR
		if( NetDefine.kakaoUserId == null )
		{
			NetBridgeLoginAuth.SendPacket();
			return;
		}
		#endif
		
		initializing = true;
		
		UpdateDailyEnergy();
		UpdateRuby();
		UpdateGameMoney();
		UpdatePresentPoint();
		
		UpdatePlayerCharacterInfo();
		UpdatePlayerPetInfo();
		
		initializing = false;
	}
	
	IEnumerator Start()
	{
		if( playerDataHolder.firstLogin )
		{
			playerDataHolder.firstLogin = false;

			DateTime now = DateTime.Now;

			foreach( var noticeInfo in NoticeList.Instance.GetNoticeList() )
			{
				while( PopupManager.Instance.imageNoticePopup.isActive )
					yield return null;

				string key = string.Format("readNotice{0}", noticeInfo.uid);
				long tick = 0;
				if( long.TryParse(PlayerPrefs.GetString(key), out tick) )
				{
					DateTime savedDate = new DateTime(tick);
					if( now.Subtract(savedDate).Days < 1 && now.Day == savedDate.Day )
					{
						continue;
					}
				}
				
				PopupManager.Instance.imageNoticePopup.Open(noticeInfo.url, ()=>{
					PlayerPrefs.SetString( key, System.DateTime.Now.Ticks.ToString() );
				});
			}

			foreach( var eventInfo in NoticeList.Instance.GetEventList() )
			{
				while( PopupManager.Instance.imageNoticePopup.isActive )
					yield return null;

				string key = string.Format("readEvent{0}", eventInfo.uid);
				long tick = 0;
				if( long.TryParse(PlayerPrefs.GetString(key), out tick) )
				{
					DateTime savedDate = new DateTime(tick);
					if( now.Subtract(savedDate).Days < 1 && now.Day == savedDate.Day )
					{
						continue;
					}
				}
				
				PopupManager.Instance.imageNoticePopup.Open(eventInfo.url, ()=>{
					PlayerPrefs.SetString( key, System.DateTime.Now.Ticks.ToString() );
				});
			}

			while( PopupManager.Instance.imageNoticePopup.isActive )
				yield return null;

			if( playerDataHolder.needRewardLastWeek )
			{
				PopupManager.Instance.SetLoadingUI(true);
				if( NetBridgeLoadFriends.SendPacket() )
				{
					while( FriendList.Instance.IsUpdating() )
						yield return null;
				}
				PopupManager.Instance.SetLoadingUI(false);

				var myRankInfo = FriendList.Instance.myRank;
				if( myRankInfo.lastSeasonScore > 0 )
				{
					lastWeekRankPopup.SetActive(true);

					while( lastWeekRankPopup.activeSelf ) 
						yield return null;

					while( PopupManager.Instance.rewardPopup.gameObject.activeSelf ) 
						yield return null;
				}
			}

			if( playerDataHolder.dailyRewardData != null )
			{

				dailyLoginRewardPopup.Open(playerDataHolder.dailyRewardData);

				while( dailyLoginRewardPopup.gameObject.activeSelf ) 
					yield return null;
				
				while( PopupManager.Instance.rewardPopup.gameObject.activeSelf ) 
					yield return null;

				playerDataHolder.dailyRewardData = null;
			}
		}
		else if( playerDataHolder.needEvaluate && playerDataHolder.readyToEvaluate )
		{
			PopupManager.Instance.yesnoPopup.ShowPopup(TextType.UI_TEXT_EVALUATE_CONFIRM,
			()=>{
				Application.OpenURL(NetDefine.appURL);

				NetBridgeEvaluateReward.SendPacket();
			}, 
			()=>{
				playerDataHolder.readyToEvaluate = false;
			});
		}
	}

	void OnClickEvaluate()
	{
	}

	public void UpdateDailyEnergy()
	{
		dailyEnergy.UpdateCarrots();
		
		dailyEnergyLabel1.text = dailyEnergyLabel2.text = playerDataHolder.gameCarrots.ToString();
	}
	
	public void UpdateRuby()
	{
		string text = string.Format("{0:n0}", playerDataHolder.gameRuby);
		if( RubyLabel.text != text )
		{
			RubyLabel.text = text;
			if( !initializing && RubyScaleAnimation.enabled == false )
			{
				RubyScaleAnimation.Reset();
				RubyScaleAnimation.Play(true);
			}
		}
	}
	
	public void UpdateGameMoney()
	{
		string text = string.Format("{0:n0}", playerDataHolder.gameMoney);
		if( GameMoneyLabel.text != text )
		{
			GameMoneyLabel.text = text;
			if( !initializing && GameMoneyScaleAnimation.enabled == false )
			{
				GameMoneyScaleAnimation.Reset();
				GameMoneyScaleAnimation.Play(true);
			}
		}
	}

	public void UpdatePresentPoint()
	{
		int presentCount = playerDataHolder.presentPoint / 100;
		int presentPoint = playerDataHolder.presentPoint % 100;

		PresentCountLabel.text = presentCount.ToString();
		PresentPointLabel.text = presentPoint.ToString();
		PresentPointSlider.sliderValue = presentPoint / 100f;
	}
	
	public void UpdatePlayerCharacterInfo()
	{
		CharactersData.CharacterInfo info = characterDataHolder.GetCharacterInfo(playerDataHolder.selectedCharacterId);
		
		playerCharacterInfo.character.UpdateCharacter( (int)info.id, info.level );
		
		CharacterTableSet.CharacterTable charTable = charTableSet.GetCharacterTable(info.id);
		CharacterTableSet.CharacterShopLevelTable levelInfo = charTable.GetLevelInfo( info.level );

		if( info.level < charTable.maxLevel )
		{
			playerCharacterInfo.levelObject.SetActive(true);
			playerCharacterInfo.levelMaxObject.SetActive(false);

			playerCharacterInfo.level.text = levelInfo.level.ToString();
			playerCharacterInfo.levelSlider.sliderValue = (float)levelInfo.level/charTable.maxLevel;
		}
		else
		{
			playerCharacterInfo.levelObject.SetActive(false);
			playerCharacterInfo.levelMaxObject.SetActive(true);
		}

		playerCharacterInfo.nameLabel.text = TextTableSet.GetText_(charTable.nameIndex);

		ShootSkill skillInfo = (ShootSkill)SkillFactory.Instance.GetSkillInfo( charTable.GetIngameLevelInfo( levelInfo.level ).shootSkillId );
		playerCharacterInfo.weaponTexture.mainTexture = Resources.Load( string.Format("Bullet/icon/{0}", skillInfo.resourceName) ) as Texture;

		for(int i=0; i<playerCharacterInfo.heartsObject.Length; i++)
		{
			playerCharacterInfo.heartsObject[i].SetActive( i < levelInfo.healthPoint );
		}
	}
	
	public void UpdatePlayerPetInfo()
	{
		for( int i=0; i<2; i++ )
		{
			int petUID = playerDataHolder.selectedPet[i];
			playerPetInfo.petSlot[i].UpdatePet(petUID);
		}
	}
	
	public string GetRandomName()
	{
		return textTableSet.GetText(UnityEngine.Random.Range(990001, 990021));
	}
}
