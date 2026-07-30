import 'package:flutter/material.dart';
import 'package:launchpad_flutter/screens/home_screen.dart';
import 'package:launchpad_flutter/src/rust/frb_generated.dart';
import 'package:launchpad_flutter/theme.dart';

Future<void> main() async {
  await RustLib.init();
  runApp(const App());
}

class App extends StatelessWidget {
  const App({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'WT Launcher',
      debugShowCheckedModeBanner: false,
      theme: buildAppTheme(isDark: false),
      darkTheme: buildAppTheme(isDark: true),
      themeMode: ThemeMode.system,
      home: const HomeScreen(),
    );
  }
}
