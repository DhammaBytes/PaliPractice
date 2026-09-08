// PaliMenu.m — Native macOS menu bar for PāliPractice
//
// WHY THIS EXISTS:
// Uno Platform's Skia desktop backend renders everything in a single NSWindow but
// does not create a native macOS menu bar. Without this, the app shows a blank menu
// bar with no standard items (no Quit, Copy/Paste, Full Screen, etc.).
//
// HOW IT WORKS:
// This file is compiled to libPaliMenu.dylib by MacMenu.targets (clang, at build
// time on macOS). At app startup, MacMenuBridge.cs loads the dylib via P/Invoke
// and calls pali_menu_install(), passing a C function pointer. This function builds
// the NSMenu hierarchy and assigns it to NSApp.mainMenu.
//
// Menu items fall into two categories:
//   1. Standard items (Edit, View, Window) — use nil target so the macOS responder
//      chain routes them to the first responder (Uno's text fields, etc.).
//   2. Custom items (About, Settings, Help) — target a PaliMenuTarget object that
//      calls back into C# with a string identifier. MacMenuBridge.cs then navigates
//      to the corresponding page via Uno Navigation Extensions.

#import <AppKit/AppKit.h>
#include <string.h>

// Callback type: C# provides a function pointer that receives a C string action identifier.
typedef void (*MenuCallback)(const char *action);

static MenuCallback g_callback = NULL;

// ---------------------------------------------------------------------------
// PaliMenuTarget — ObjC class that receives menu item actions and forwards
// them to the C# callback via the registered function pointer.
// ---------------------------------------------------------------------------
@interface PaliMenuTarget : NSObject
- (void)menuAction:(id)sender;
@end

@implementation PaliMenuTarget

- (void)menuAction:(id)sender {
    if (!g_callback) return;
    NSMenuItem *item = (NSMenuItem *)sender;
    // Each custom menu item stores its action identifier (e.g. "about", "settings")
    // as representedObject. We pass this string back to C# for dispatch.
    NSString *tagStr = (NSString *)item.representedObject;
    const char *tag = tagStr.UTF8String;
    if (tag) g_callback(tag);
}

@end

static PaliMenuTarget *g_target = nil;

// ---------------------------------------------------------------------------
// Helper: create a menu item that calls back into C# with a string tag.
// Used for app-specific actions (About, Settings, Help).
// ---------------------------------------------------------------------------
static NSMenuItem *customItem(NSString *title, NSString *key, NSEventModifierFlags mods, NSString *tag) {
    NSMenuItem *item = [[NSMenuItem alloc] initWithTitle:title
                                                 action:@selector(menuAction:)
                                          keyEquivalent:key];
    item.target = g_target;
    item.keyEquivalentModifierMask = mods;
    item.representedObject = tag;
    return item;
}

// ---------------------------------------------------------------------------
// Helper: create a menu item that uses the macOS responder chain (nil target).
// Used for standard actions (Copy, Paste, Full Screen, etc.) that the first
// responder handles automatically.
// ---------------------------------------------------------------------------
static NSMenuItem *stdItem(NSString *title, SEL action, NSString *key, NSEventModifierFlags mods) {
    NSMenuItem *item = [[NSMenuItem alloc] initWithTitle:title
                                                 action:action
                                          keyEquivalent:key];
    item.target = nil; // responder chain
    item.keyEquivalentModifierMask = mods;
    return item;
}

// ---------------------------------------------------------------------------
// Build the complete menu bar and assign it to NSApp.
// ---------------------------------------------------------------------------
static void buildMenuBar(NSDictionary<NSString *, NSString *> *labels) {
    NSMenu *mainMenu = [[NSMenu alloc] init];

    // ---- App menu (bold app name in menu bar) ----
    {
        NSMenu *appMenu = [[NSMenu alloc] initWithTitle:@"Pāli Practice"];

        [appMenu addItem:customItem(labels[@"About"], @"", 0, @"about")];
        [appMenu addItem:[NSMenuItem separatorItem]];
        [appMenu addItem:customItem(labels[@"Settings"], @",", NSEventModifierFlagCommand, @"settings")];
        [appMenu addItem:[NSMenuItem separatorItem]];
        [appMenu addItem:stdItem(labels[@"Hide"], @selector(hide:), @"h", NSEventModifierFlagCommand)];
        [appMenu addItem:stdItem(labels[@"HideOthers"], @selector(hideOtherApplications:), @"h",
                                 NSEventModifierFlagCommand | NSEventModifierFlagOption)];
        [appMenu addItem:stdItem(labels[@"ShowAll"], @selector(unhideAllApplications:), @"", 0)];
        [appMenu addItem:[NSMenuItem separatorItem]];
        [appMenu addItem:stdItem(labels[@"Quit"], @selector(terminate:), @"q", NSEventModifierFlagCommand)];

        NSMenuItem *appMenuItem = [[NSMenuItem alloc] init];
        appMenuItem.submenu = appMenu;
        [mainMenu addItem:appMenuItem];
    }

    // ---- Edit menu (responder chain — works with Uno's text input) ----
    {
        NSMenu *editMenu = [[NSMenu alloc] initWithTitle:labels[@"Edit"]];

        [editMenu addItem:stdItem(labels[@"Undo"], @selector(undo:), @"z", NSEventModifierFlagCommand)];
        [editMenu addItem:stdItem(labels[@"Redo"], @selector(redo:), @"z",
                                  NSEventModifierFlagCommand | NSEventModifierFlagShift)];
        [editMenu addItem:[NSMenuItem separatorItem]];
        [editMenu addItem:stdItem(labels[@"Cut"], @selector(cut:), @"x", NSEventModifierFlagCommand)];
        [editMenu addItem:stdItem(labels[@"Copy"], @selector(copy:), @"c", NSEventModifierFlagCommand)];
        [editMenu addItem:stdItem(labels[@"Paste"], @selector(paste:), @"v", NSEventModifierFlagCommand)];
        [editMenu addItem:stdItem(labels[@"Delete"], @selector(delete:), @"", 0)];
        [editMenu addItem:[NSMenuItem separatorItem]];
        [editMenu addItem:stdItem(labels[@"SelectAll"], @selector(selectAll:), @"a", NSEventModifierFlagCommand)];

        NSMenuItem *editMenuItem = [[NSMenuItem alloc] init];
        editMenuItem.submenu = editMenu;
        [mainMenu addItem:editMenuItem];
    }

    // ---- View menu ----
    {
        NSMenu *viewMenu = [[NSMenu alloc] initWithTitle:labels[@"View"]];

        [viewMenu addItem:stdItem(labels[@"FullScreen"], @selector(toggleFullScreen:), @"f",
                                  NSEventModifierFlagCommand | NSEventModifierFlagControl)];

        NSMenuItem *viewMenuItem = [[NSMenuItem alloc] init];
        viewMenuItem.submenu = viewMenu;
        [mainMenu addItem:viewMenuItem];
    }

    // ---- Window menu (setWindowsMenu: lets macOS auto-add open window names) ----
    {
        NSMenu *windowMenu = [[NSMenu alloc] initWithTitle:labels[@"Window"]];

        [windowMenu addItem:stdItem(labels[@"Minimize"], @selector(performMiniaturize:), @"m", NSEventModifierFlagCommand)];
        [windowMenu addItem:stdItem(labels[@"Zoom"], @selector(performZoom:), @"", 0)];
        [windowMenu addItem:[NSMenuItem separatorItem]];
        [windowMenu addItem:stdItem(labels[@"BringAllToFront"], @selector(arrangeInFront:), @"", 0)];

        NSMenuItem *windowMenuItem = [[NSMenuItem alloc] init];
        windowMenuItem.submenu = windowMenu;
        [mainMenu addItem:windowMenuItem];

        [NSApp setWindowsMenu:windowMenu];
    }

    // ---- Help menu (setHelpMenu: enables macOS help search field) ----
    {
        NSMenu *helpMenu = [[NSMenu alloc] initWithTitle:labels[@"Help"]];

        [helpMenu addItem:customItem(labels[@"AppHelp"], @"", 0, @"help")];

        NSMenuItem *helpMenuItem = [[NSMenuItem alloc] init];
        helpMenuItem.submenu = helpMenu;
        [mainMenu addItem:helpMenuItem];

        [NSApp setHelpMenu:helpMenu];
    }

    [NSApp setMainMenu:mainMenu];
}

// ---------------------------------------------------------------------------
// Public C API — called once from MacMenuBridge.cs via P/Invoke at startup.
// ---------------------------------------------------------------------------
__attribute__((visibility("default")))
void pali_menu_install(MenuCallback callback, const char *labelsJson) {
    NSData *data = [[NSString stringWithUTF8String:labelsJson] dataUsingEncoding:NSUTF8StringEncoding];
    NSDictionary<NSString *, NSString *> *labels = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
    g_callback = callback;
    g_target = [[PaliMenuTarget alloc] init];

    // Disable automatic window tabbing — removes the "Show/Hide Tab Bar" item
    // that macOS auto-inserts into the View menu for tabbing-capable windows.
    if ([NSWindow respondsToSelector:@selector(setAllowsAutomaticWindowTabbing:)]) {
        NSWindow.allowsAutomaticWindowTabbing = NO;
    }

    // Menu must be built on the main thread.
    if ([NSThread isMainThread]) {
        buildMenuBar(labels);
    } else {
        dispatch_sync(dispatch_get_main_queue(), ^{
            buildMenuBar(labels);
        });
    }
}
