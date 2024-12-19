# Meta Tutorials Framework Hub
## Intro
The tutorial framework hub allows you to quickly generate in-unity tutorials that derive most of their content from your already existing documentation markdown files, such as your README.md. Once completed, the tutorial hub can be opened via the menu item: Meta > Tutorial Hub > Show Hub.

## How to create a tutorial
1. With the Meta Tutorial Framework imported into your project, add ```META_EDIT_TUTORIALS``` to the project’s Scripting Define Symbols, under the Project Settings tab’s Player section, and apply.  
2. After Unity refreshes, you should find several new dropdown menu items:  
   * Meta > Tutorial Hub > Toggle Edit Tutorials > Disable  
     Removes the ```META_EDIT_TUTORIALS``` define, and prevents tutorials from being edited or authored  
   * Right click in Assets panel > Meta Hub Tutorial > Tutorial Config  
   * Right click in Assets panel > Meta Hub Tutorial > Tutorial Markdown Context  
   * Right click in Assets panel > Meta Hub Tutorial > Tutorial References Context  
3. Author your tutorial by creating a config first, then associate it with any new tutorial context that you create to ensure your tutorial pages appear grouped together, since the hub is capable of showing more than one tutorial.  
4. Preview your results via: Meta > Tutorial Hub > Show Hub.  
5. Once you are happy with the tutorial you’ve created, delete the pages folder that is created right next to the context files, and open the tutorial one more time to ensure all generated page assets have been correctly created and associated with all necessary components.  
6. Disable tutorial editing mode using the menu item mentioned above. 

## Components details
### Tutorial Config
![Tutorial Config](./documentation/images/TutCfg.png)

### Tutorial Markdown Context - display markdown pages
| Field | Description |
| ----- | ----- |
| Title | The name of the page that is listed in the nav bar on the left |
| Priority | Used to determine the order of the page in the list. Lower values appear closer to the top. |
| Tutorial Config | Must be assigned |
| Show Banner | Determines whether or not to render the banner at the top of the page when the page is selected |
| Markdown Path | Path to .md file that will populate this page and/or child pages. Path is relative to project root (parent of Assets folder). Once a valid path is entered, the Page Configs field will populate with all sections separated by level 1 headers. |
| Page Configs | Section titles cannot be renamed, but the checkboxes for each can be edited. Currently there are only 2 supported modes of use: All (or some) sections are marked to appear in context, and none are marked to appear as children. All (or some) sections are marked to appear as children, and none are marked to appear in context. Combining content to appear in both modes may produce unintended behaviors. |

### Tutorial References Context - list out important objects to be highlighted
| Field | Description |
| ----- | ----- |
| Title | The name of the page that is listed in the nav bar on the left |
| Priority | Used to determine the order of the page in the list. Lower values appear closer to the top. |
| Tutorial Config | Must be assigned |
| Show Banner | Determines whether or not to render the banner at the top of the page when the page is selected |
| References | Entries in this list can be used to highlight and select assets or scene objects. Each one gets a header(1), a description(2), and a reference(3) that needs to be set up. By default, that field is set to the Reference Type of Serialized Object(4), and the Object field is empty. You can drag & drop both assets and scene GameObjects into that Object field. If you drag a GameObject, it will reconfigure the Reference Type to Scene Object and write out the object’s path in the Name field. |

![References Context](./documentation/images/RefCtx.png)
