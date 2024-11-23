#### Additional Notes:
<GlobalInfo> these permissions are moved to the profile display since they cannot be modified

Settings Viewable in profile window: (maybe add functionality so some settings only show when fully paired?)
- Commands from Friends
- Commands from Party
- Wardrobe Enabled (maybe move the auto equips to per-set permissions?
- Puppeteer Enabled
- Puppeteer Global Trigger Phrase
- Puppeteer Global Sit
- Puppeteer Global Motion
- Puppeteer Global All
- Toybox Enabled
- Chat Garbler Channels (difficult)
- Puppeteer Channels (difficult)
- Spacial Vibrator Audio
(Hardcore Mode is turned to per pair)


### Settings Button StickyPermsUi content
** Tabs to toggle between viewing *your settings* and the *other user's settings* for permissions.
	<Global Settings>
	- Live Chat Garbler
	- Live Chat Garbler Lock
	- Toybox UI Lock
	<Gag Permissions>
	- Gag Item Auto-Equip (Applies gag glamours when this pair applies a gag to you)
	- Max Lock duration (maybe?)
	- Extended Lock Times
	<Wardrobe permissions> (lock gag storage on gag lock will become default?)
	- Restraint Set Applying
	- Restraint Set Locking
	- Restraint Set Max Lock Duration
	- Restraint Set Removal
	<Puppeteer Permissions>
	- Sit Commands Allowed
	- Motion Commands Allowed
	- All Commands Allowed (maybe some checks to prevent commands not from the game? IDK)
	<Moodles>
	- Positive Status Types Allowed
	- Negative Status Types Allowed
	- Special Status Types Allowed
	- Can Apply target Pair's Moodles
	- Can Apply own Moodles to target Pair
	- Max Allowed Moodles duration
	- Allow Perminant Moodles Effects
	<Toybox>
	- Can Change Toy State
	- Can Control Intensity
	- Can Use Real-Time Vibrator remote
	- Can Execute Patterns
	- Can Control Triggers
	- Can Create Triggers
	<Hardcore> 
	- Follow Order allowed (READONLY)
	- Follow Order state
	- Sit Order allowed (READONLY)
	- Sit Order state
	- Lock Away Order allowed (READONLY)
	- Lock Away Order state
	- Blindfold Order allowed (READONLY)
	- Blindfold Forces 1st Person (READONLY)
	- Blindfold Order state
	(Hardcore Restraint Set Properties are provided in restraint set / apperance window)
	
### Triple Dot Menu Interactions (meant for application of things, beyond permissions)
	<Gag Interactions>
	- Underlayer >>
		- Apply Gag >>
			- (Display List of Gags)
		- Lock Gag >>
			- (List of Lock Types) >>
				- (Password Insertion Field here if any, possibly in a popup)
		- Unlock Gag >>
			- (List of Lock Types) >>
				- (Password Insertion Field here if any, possibly in a popup)
		- Remove Gag on this Layer (only visible if no lock present)
		- Remove All Gags
	- Middle Layer >>
		- [ Same Application as Underlayer ]
	- Top layer >>
		- [ Same Application as Underlayer ]
	
	<RestraintSet Interactions>
	- Inspect Active Restraint Set     // PairApperanceUi
	- Enable Restraint Set >>
		- (Display List of Pairs Restraint Sets)
	- Lock Restraint Set >>
		- Display List of Restraint Sets) >>
			- (Display Lock duration prompt)
	- Unlock active Restraint Set
	- Disable active Restraint Set
	- Unlock & Disable active Restraint Set
	<Puppeteer Interactions>
	- Alias List >>
		- (List of user pair's Aliases they set for you) >>
			- Copy alias to clipboard
	<Toybox Interactions>
	- Vibrator Remote >>
		- Open (opens realtime vibrator to user on pair)
	- Patterns >>
		- (Select a pattern from the pairs pattern list) >> **Note that Patterns will have loop and currently active marks by them**
			- Set Pattern Loop Status
			- Execute Pattern
		- Halt Active Pattern
	[ Feature Creep Things ]
	- Viberator Alarms >>
		- Pair's Stored Alarms >>
			- Enable Alarm
			- Change Assigned Pattern >>
				- (Display Pattern List)
			- Disable Alarm
		- Send Alarm to Pair >>
			- (Display your created alarm list)
	
	- Viberator Triggers >>
		- Triggers >> 
			- (Displays Trigger List) >>
				- Enable (shown when disabled)
				- Disable (shown when enabled)
		- Send Trigger >>
			- (Display your trigger list) >>
				- (Press on trigger to send to pair)
		
## Mental notes to avoid brain imploding:

- Wardrobe Appearance, Trigger Alias's Pattern and Trigger data will still
be stored locally.

- This information would cause database bloat, and is not necessary to see while
offline.

- Instead, this information will be stored in a locally accessed client set 
characterData file, which is saved to config.

- This file will know which user it is associated with by defining the respective UID in storage

- This config save will work similarly to the way that the client saves server labels/tags,
and server nicknames, and configurations
		
		
		
		
